using bloodPressureWebApi.Data;
using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Experiments.E4;
using bloodPressureWebApi.Experiments.E5.FaultInjection;
using bloodPressureWebApi.Experiments.E7;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace bloodPressureWebApi.Experiments.E5
{
    /// <summary>
    /// E5 - Inyeccion de fallos y resiliencia.
    ///
    /// QUE EVALUA (y que no)
    ///   El fallo se inyecta en AIDiagnosisService justo ANTES de invocar el
    ///   motor cientifico (MATLAB). El backend no tiene reintentos ni circuit
    ///   breaker, asi que E5 no mide "recuperacion automatica"; mide:
    ///     1. Aislamiento: un fallo afecta solo a la ejecucion donde ocurre.
    ///     2. Trazabilidad: cada fallo queda registrado con su excepcion.
    ///     3. Integridad: las ejecuciones exitosas (durante y despues de los
    ///        fallos) producen exactamente el resultado de la linea base, y una
    ///        pasada limpia posterior confirma que no quedo estado corrupto.
    ///
    /// CAMBIOS RESPECTO A LA VERSION ANTERIOR
    ///   - Todos los escenarios en una sola corrida, con linea base primero.
    ///   - Estudios: todos los que tienen features (antes StudyStatus=="ANALYZED",
    ///     que dependia de que E4 se hubiera corrido antes).
    ///   - try/finally: el inyector SIEMPRE se desactiva (antes, si algo fallaba
    ///     a la mitad, quedaba activo y contaminaba E4/E6/E7).
    ///   - Comparacion de cada resultado exitoso contra la linea base.
    ///   - ChangeTracker.Clear() tras cada diagnostico (el DbContext acumulaba
    ///     entidades durante miles de ejecuciones).
    /// </summary>
    public class E5ExperimentRunner
    {
        private readonly ApplicationDbContext _context;
        private readonly AIDiagnosisService _diagnosisService;
        private readonly IFaultInjectionService _faultInjection;
        private readonly OrchestrationHarness _harness;

        public E5ExperimentRunner(
            ApplicationDbContext context,
            AIDiagnosisService diagnosisService,
            IFaultInjectionService faultInjection,
            OrchestrationHarness harness)
        {
            _context = context;
            _diagnosisService = diagnosisService;
            _faultInjection = faultInjection;
            _harness = harness;
        }

        public async Task<E5ExperimentResultDto> RunAsync(E5ExperimentRequestDto request)
        {
            var result = new E5ExperimentResultDto
            {
                StartedAt = DateTime.UtcNow,
                Iterations = request.Iterations
            };

            var totalTimer = Stopwatch.StartNew();

            // Seguridad: el inyector debe empezar apagado
            _faultInjection.Disable();

            List<int> studyIds = await LoadStudyIdsAsync(request);
            result.StudiesProcessed = studyIds.Count;

            // Escenarios: None siempre primero (linea base)
            var scenarios = (request.Scenarios != null && request.Scenarios.Count > 0
                    ? request.Scenarios
                    : new List<FaultScenario> { request.Scenario })
                .Distinct()
                .Where(s => s != FaultScenario.None)
                .ToList();
            scenarios.Insert(0, FaultScenario.None);

            var records = new List<E5ExecutionRecord>();
            var baseline = new Dictionary<int, string>();

            foreach (FaultScenario scenario in scenarios)
            {
                var scenarioRecords = new List<E5ExecutionRecord>();

                for (int iteration = 1; iteration <= request.Iterations; iteration++)
                {
                    try
                    {
                        if (scenario != FaultScenario.None)
                        {
                            _faultInjection.Configure(scenario, iteration);
                        }

                        int position = 0;

                        foreach (int studyId in studyIds)
                        {
                            position++;
                            var rec = await ExecuteOneAsync(scenario, iteration, position, studyId);

                            // La primera ejecucion exitosa de la linea base define la referencia
                            if (scenario == FaultScenario.None && rec.Success &&
                                !baseline.ContainsKey(studyId))
                            {
                                baseline[studyId] = rec.OutputHash;
                            }

                            rec.MatchesBaseline = rec.Success &&
                                baseline.TryGetValue(studyId, out var b) &&
                                b == rec.OutputHash;

                            scenarioRecords.Add(rec);
                        }
                    }
                    finally
                    {
                        _faultInjection.Disable();
                    }
                }

                // Pasada limpia posterior (no aplica a la linea base)
                int verifyMatches = 0;
                if (scenario != FaultScenario.None)
                {
                    int position = 0;
                    foreach (int studyId in studyIds)
                    {
                        position++;
                        var rec = await ExecuteOneAsync(scenario, 0, position, studyId);
                        rec.Phase = "PostFaultVerification";
                        rec.MatchesBaseline = rec.Success &&
                            baseline.TryGetValue(studyId, out var b) &&
                            b == rec.OutputHash;
                        if (rec.MatchesBaseline) verifyMatches++;
                        scenarioRecords.Add(rec);
                    }
                }

                var measured = scenarioRecords.Where(r => r.Phase == "Scenario").ToList();
                string expectedMessage = ExpectedMessageFragment(scenario);

                result.Scenarios.Add(new E5ScenarioSummary
                {
                    Scenario = scenario.ToString(),
                    Executions = measured.Count,
                    Successful = measured.Count(r => r.Success),
                    Failed = measured.Count(r => !r.Success),
                    SuccessRatePercent = measured.Count > 0
                        ? 100.0 * measured.Count(r => r.Success) / measured.Count : 0,
                    FailuresMatchingInjectedFault = measured.Count(r =>
                        !r.Success && expectedMessage.Length > 0 &&
                        r.ExceptionMessage.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)),
                    SuccessfulMatchingBaseline = measured.Count(r => r.MatchesBaseline),
                    MedianSuccessMs = Median(measured.Where(r => r.Success).Select(r => r.DurationMs)),
                    MedianFailureMs = Median(measured.Where(r => !r.Success).Select(r => r.DurationMs)),
                    PostFaultVerificationMatches = verifyMatches,
                    PostFaultVerificationStudies = scenario == FaultScenario.None ? 0 : studyIds.Count
                });

                // Fallos no inyectados = inesperados
                foreach (var r in scenarioRecords.Where(r => !r.Success &&
                             (r.Phase != "Scenario" || scenario == FaultScenario.None ||
                              expectedMessage.Length == 0 ||
                              !r.ExceptionMessage.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase))))
                {
                    result.UnexpectedErrors.Add(
                        $"{r.Scenario} {r.Phase} it{r.Iteration} study {r.StudyId}: {r.ExceptionType}: {r.ExceptionMessage}");
                }

                records.AddRange(scenarioRecords);
            }

            // -------------------------------------------------
            // NUEVO: recuperacion real a traves de la cola (reintentos)
            // -------------------------------------------------
            var queueBatches = new List<BatchRecord>();
            var queueJobs = new List<JobRecord>();

            if (request.IncludeQueueRecovery)
            {
                foreach (var kv in baseline) _harness.Baseline[kv.Key] = kv.Value;

                var queueConfig = OrchestrationConfig.Async(1);

                foreach (FaultScenario scenario in scenarios.Where(s => s != FaultScenario.None))
                {
                    var scenarioJobs = new List<JobRecord>();
                    var makespans = new List<double>();

                    for (int iteration = 1; iteration <= request.Iterations; iteration++)
                    {
                        try
                        {
                            _faultInjection.Configure(scenario, iteration);
                            var (batch, jobs) = await _harness.RunBatchAsync(
                                queueConfig with { Name = $"Queue_{scenario}" },
                                studyIds, iteration, 1);
                            queueBatches.Add(batch);
                            scenarioJobs.AddRange(jobs);
                            makespans.Add(batch.MakespanMs);
                        }
                        finally
                        {
                            _faultInjection.Disable();
                            _harness.ResetWorker();
                        }
                    }

                    queueJobs.AddRange(scenarioJobs);

                    result.QueueRecovery.Add(new E5QueueRecoverySummary
                    {
                        Scenario = scenario.ToString(),
                        Jobs = scenarioJobs.Count,
                        EventuallySuccessful = scenarioJobs.Count(j => j.Success),
                        RecoveredByRetry = scenarioJobs.Count(j => j.Success && j.Attempts > 1),
                        FinallyFailed = scenarioJobs.Count(j => !j.Success),
                        MeanAttempts = scenarioJobs.Count > 0 ? scenarioJobs.Average(j => j.Attempts) : 0,
                        MaxAttempts = scenarioJobs.Count > 0 ? scenarioJobs.Max(j => j.Attempts) : 0,
                        SuccessfulMatchingBaseline = scenarioJobs.Count(j => j.MatchesBaseline),
                        MedianMakespanMs = Median(makespans)
                    });
                }
            }

            string outputFolder = ExperimentPaths.CreateOutputFolder("E5");
            result.OutputFolder = outputFolder;

            if (request.ExportCsv)
            {
                ExportRecords(records, Path.Combine(outputFolder, "ExecutionRecords.csv"));
                ExportSummary(result.Scenarios, Path.Combine(outputFolder, "ScenarioSummary.csv"));

                if (queueJobs.Count > 0)
                {
                    OrchestrationHarness.ExportBatches(queueBatches, Path.Combine(outputFolder, "QueueBatches.csv"));
                    OrchestrationHarness.ExportJobs(queueJobs, Path.Combine(outputFolder, "QueueJobRecords.csv"));
                }
            }

            totalTimer.Stop();
            result.TotalExecutionTimeSeconds = totalTimer.Elapsed.TotalSeconds;
            result.FinishedAt = DateTime.UtcNow;
            return result;
        }

        // =====================================================
        // UNA EJECUCION
        // =====================================================

        private async Task<E5ExecutionRecord> ExecuteOneAsync(
            FaultScenario scenario, int iteration, int position, int studyId)
        {
            var rec = new E5ExecutionRecord
            {
                Scenario = scenario.ToString(),
                Phase = "Scenario",
                Iteration = iteration,
                PositionInIteration = position,
                StudyId = studyId,
                StartedAtUtc = DateTime.UtcNow
            };

            var sw = Stopwatch.StartNew();
            try
            {
                await _diagnosisService.RunDiagnosis(studyId, includeFramingham: false);
                sw.Stop();

                var diagnosis = await _context.AIDiagnosis.AsNoTracking()
                    .FirstAsync(x => x.StudyId == studyId);

                rec.Success = true;
                rec.OutputHash = ResultSignatureBuilder.ComputeHash(
                    ResultSignatureBuilder.BuildOutputSignature(diagnosis));
            }
            catch (Exception ex)
            {
                sw.Stop();
                rec.Success = false;
                rec.ExceptionType = ex.GetType().Name;
                rec.ExceptionMessage = ex.Message;
            }
            finally
            {
                // Evita que el DbContext acumule entidades durante la corrida
                _context.ChangeTracker.Clear();
            }

            rec.DurationMs = sw.Elapsed.TotalMilliseconds;
            return rec;
        }

        private async Task<List<int>> LoadStudyIdsAsync(E5ExperimentRequestDto request)
        {
            IQueryable<MAPAStudy> query = _context.MAPAStudies
                .AsNoTracking()
                .Where(s => s.Features != null);

            if (request.StudyIds != null && request.StudyIds.Count > 0)
            {
                query = query.Where(s => request.StudyIds.Contains(s.StudyId));
            }

            var ids = await query.OrderBy(s => s.StudyId).Select(s => s.StudyId).ToListAsync();

            if (ids.Count == 0)
            {
                throw new InvalidOperationException("E5: no hay estudios con features.");
            }

            return ids;
        }

        private static string ExpectedMessageFragment(FaultScenario s) => s switch
        {
            FaultScenario.MatlabFailure => "Simulated AI Engine failure",
            FaultScenario.IntermittentFailure => "Intermittent AI Engine failure",
            FaultScenario.PersistentFailure => "Persistent AI Engine failure",
            FaultScenario.RecoveryFailure => "Recovery AI Engine failure",
            _ => string.Empty
        };

        // null cuando no hay datos (p. ej. no hubo fallos en la linea base):
        // NaN no se puede serializar a JSON y hacia fallar la respuesta.
        private static double? Median(IEnumerable<double> values)
        {
            var v = values.OrderBy(x => x).ToList();
            if (v.Count == 0) return null;
            int m = v.Count / 2;
            return v.Count % 2 == 1 ? v[m] : (v[m - 1] + v[m]) / 2.0;
        }

        // =====================================================
        // EXPORT
        // =====================================================

        private sealed class E5ExecutionRecord
        {
            public string Scenario { get; set; } = "";
            public string Phase { get; set; } = "";
            public int Iteration { get; set; }
            public int PositionInIteration { get; set; }
            public int StudyId { get; set; }
            public DateTime StartedAtUtc { get; set; }
            public bool Success { get; set; }
            public double DurationMs { get; set; }
            public string ExceptionType { get; set; } = "";
            public string ExceptionMessage { get; set; } = "";
            public string OutputHash { get; set; } = "";
            public bool MatchesBaseline { get; set; }
        }

        private static string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        private static void ExportRecords(IEnumerable<E5ExecutionRecord> rows, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
            var inv = CultureInfo.InvariantCulture;
            w.WriteLine("Scenario,Phase,Iteration,PositionInIteration,StudyId,StartedAtUtc,Success," +
                        "DurationMs,ExceptionType,ExceptionMessage,OutputHash,MatchesBaseline");
            foreach (var r in rows)
            {
                w.WriteLine(string.Join(",",
                    Q(r.Scenario), Q(r.Phase), r.Iteration, r.PositionInIteration, r.StudyId,
                    Q(r.StartedAtUtc.ToString("O", inv)), r.Success ? "true" : "false",
                    r.DurationMs.ToString("R", inv), Q(r.ExceptionType), Q(r.ExceptionMessage),
                    Q(r.OutputHash), r.MatchesBaseline ? "true" : "false"));
            }
        }

        private static void ExportSummary(IEnumerable<E5ScenarioSummary> rows, string path)
        {
            using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
            var inv = CultureInfo.InvariantCulture;
            w.WriteLine("Scenario,Executions,Successful,Failed,SuccessRatePercent," +
                        "FailuresMatchingInjectedFault,SuccessfulMatchingBaseline," +
                        "MedianSuccessMs,MedianFailureMs,PostFaultVerificationMatches,PostFaultVerificationStudies");
            foreach (var s in rows)
            {
                w.WriteLine(string.Join(",",
                    Q(s.Scenario), s.Executions, s.Successful, s.Failed,
                    s.SuccessRatePercent.ToString("R", inv),
                    s.FailuresMatchingInjectedFault, s.SuccessfulMatchingBaseline,
                    s.MedianSuccessMs?.ToString("R", inv) ?? "", s.MedianFailureMs?.ToString("R", inv) ?? "",
                    s.PostFaultVerificationMatches, s.PostFaultVerificationStudies));
            }
        }
    }
}
