using bloodPressureWebApi.Data;
using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Experiments.E2;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;

using Microsoft.EntityFrameworkCore;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace bloodPressureWebApi.Experiments.E3
{
    /// <summary>
    /// E3 - Ejecucion concurrente del pipeline de diagnostico.
    ///
    /// CAMBIOS RESPECTO A LA VERSION ANTERIOR
    ///  1. Usa DiagnosisDataLoader (igual que E2): 50 estudios, 47 lecturas,
    ///     edad 40 por defecto, y la red neuronal (BPTrend) SI se ejecuta.
    ///     (Sin esto, con el DiagnosisBenchmark corregido E3 fallaba.)
    ///  2. Barrido de niveles de concurrencia en una sola corrida
    ///     (p. ej. 1, 2, 4, 8). El nivel 1 es la linea base secuencial.
    ///  3. Mide lo que importa en concurrencia: el tiempo de pared del lote
    ///     (makespan) y el throughput por iteracion -> IterationSummary.csv,
    ///     y la latencia de pared de cada diagnostico -> DiagnosisResults.csv.
    ///  4. Un profiler POR DIAGNOSTICO (antes uno compartido por todos los
    ///     hilos; si su lista interna no es thread-safe se pierden o corrompen
    ///     mediciones). Las metricas se juntan al final.
    ///  5. WarmStart ahora SI calienta: una pasada secuencial no medida antes
    ///     de empezar. Antes solo cambiaba una etiqueta.
    ///  6. Verifica que la concurrencia no altere los resultados: la salida
    ///     de la ANN se compara con la tendencia guardada (TrendMatchesStored).
    /// </summary>
    public class E3ExperimentRunner
    {
        private readonly ApplicationDbContext _context;
        private readonly MatlabService _matlab;

        public E3ExperimentRunner(
            ApplicationDbContext context,
            MatlabService matlab)
        {
            _context = context;
            _matlab = matlab;
        }

        public async Task<E3ExperimentResultDto> RunAsync(
            E3ExperimentRequestDto request)
        {
            var result = new E3ExperimentResultDto
            {
                StartedAt = DateTime.Now
            };

            var totalTimer = Stopwatch.StartNew();

            // -------------------------------------------------
            // Niveles de concurrencia
            // -------------------------------------------------
            List<int> levels = (request.ConcurrencyLevels != null &&
                                request.ConcurrencyLevels.Count > 0
                    ? request.ConcurrencyLevels
                    : new List<int> { request.Concurrency })
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (levels.Count == 0)
            {
                throw new InvalidOperationException(
                    "E3: se requiere al menos un nivel de concurrencia > 0.");
            }

            result.ConcurrencyLevels = levels;
            result.Concurrency = levels[0];

            // -------------------------------------------------
            // Estudios (mismo criterio que E2)
            // -------------------------------------------------
            IQueryable<MAPAStudy> query = _context
                .MAPAStudies
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.RiskFactors)
                .Include(x => x.Features)
                .Include(x => x.Readings)
                .Where(x => x.Features != null);

            if (request.StudyIds != null && request.StudyIds.Count > 0)
            {
                query = query.Where(x => request.StudyIds.Contains(x.StudyId));
            }

            List<MAPAStudy> studies = await query
                .OrderBy(x => x.StudyId)
                .ToListAsync();

            if (studies.Count == 0)
            {
                throw new InvalidOperationException(
                    "E3: no hay estudios con features para medir.");
            }

            result.StudiesSelected = studies.Count;

            // Los datos se preparan UNA vez, fuera de la zona medida.
            List<DiagnosisData> inputs = studies
                .Select(DiagnosisDataLoader.Load)
                .ToList();

            // -------------------------------------------------
            // Calentamiento (no medido)
            // -------------------------------------------------
            if (request.WarmStart)
            {
                var warmTimer = Stopwatch.StartNew();
                var warmProfiler = NewProfiler(0, 0, 1, false);
                var warmBenchmark = new DiagnosisBenchmark(_matlab, warmProfiler);

                foreach (DiagnosisData data in inputs)
                {
                    warmBenchmark.Execute(data);
                }

                warmTimer.Stop();
                result.WarmUpSeconds = warmTimer.Elapsed.TotalSeconds;
            }

            // -------------------------------------------------
            // Ejecucion medida
            // -------------------------------------------------
            var allResults = new List<E3DiagnosisRecord>();
            var allMetrics = new List<StageMetric>();
            var allErrors = new List<string>();

            foreach (int level in levels)
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = level
                };

                for (int iteration = 1; iteration <= request.Iterations; iteration++)
                {
                    var records = new ConcurrentBag<E3DiagnosisRecord>();
                    var metrics = new ConcurrentBag<List<StageMetric>>();
                    var errors = new ConcurrentBag<string>();

                    DateTime batchStartUtc = DateTime.UtcNow;
                    var batchTimer = Stopwatch.StartNew();

                    await Parallel.ForEachAsync(
                        inputs,
                        options,
                        (data, token) =>
                        {
                            try
                            {
                                token.ThrowIfCancellationRequested();

                                // Profiler propio de este diagnostico (sin estado compartido)
                                var profiler = NewProfiler(level, iteration, level, !request.WarmStart);
                                var benchmark = new DiagnosisBenchmark(_matlab, profiler);

                                var wall = Stopwatch.StartNew();
                                DiagnosisBenchmarkResult diagnosis = benchmark.Execute(data);
                                wall.Stop();

                                diagnosis.Iteration = iteration;

                                records.Add(new E3DiagnosisRecord
                                {
                                    ConcurrencyLevel = level,
                                    Result = diagnosis,
                                    WallClockMs = wall.Elapsed.TotalMilliseconds,
                                    ManagedThreadId = Environment.CurrentManagedThreadId
                                });

                                metrics.Add(profiler.Metrics.ToList());
                            }
                            catch (Exception ex)
                            {
                                errors.Add(
                                    $"Level {level} - Iteration {iteration} - Study {data.StudyId}: {ex.Message}");
                            }

                            return ValueTask.CompletedTask;
                        });

                    batchTimer.Stop();

                    double makespan = batchTimer.Elapsed.TotalMilliseconds;

                    result.Iterations.Add(new E3IterationSummary
                    {
                        ConcurrencyLevel = level,
                        Iteration = iteration,
                        Studies = records.Count,
                        Errors = errors.Count,
                        MakespanMs = makespan,
                        ThroughputStudiesPerSecond = makespan > 0
                            ? records.Count / (makespan / 1000.0)
                            : 0,
                        StartedAtUtc = batchStartUtc
                    });

                    allResults.AddRange(records.OrderBy(x => x.Result.StudyId));
                    foreach (var list in metrics) allMetrics.AddRange(list);
                    allErrors.AddRange(errors);
                }
            }

            result.StudiesProcessed = allResults.Count;
            result.Errors = allErrors;

            // -------------------------------------------------
            // Exportar
            // -------------------------------------------------
            string outputFolder = ExperimentPaths.CreateOutputFolder("E3");

            // (El orden cronologico lo reconstruye el analisis con StartTime.)
            CsvExporter.ExportMetrics(
                allMetrics,
                Path.Combine(outputFolder, "StageMetrics.csv"));

            ExportDiagnosisResults(
                allResults,
                Path.Combine(outputFolder, "DiagnosisResults.csv"));

            ExportIterationSummary(
                result.Iterations,
                Path.Combine(outputFolder, "IterationSummary.csv"));

            totalTimer.Stop();

            result.TotalExecutionTimeSeconds = totalTimer.Elapsed.TotalSeconds;
            result.OutputFolder = outputFolder;
            result.MatlabFirstInitializationMs = MatlabService.FirstInitializationMs;
            result.MatlabInstancesCreated = MatlabService.InstancesCreated;

            result.FinishedAt = DateTime.Now;

            return result;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static ExperimentProfiler NewProfiler(
            int level, int iteration, int concurrency, bool coldStart)
        {
            return new ExperimentProfiler(
                experimentName: "E3",
                scenario: level == 0 ? "WarmUp" : $"Concurrent_{level}",
                iteration: iteration,
                coldStart: coldStart,
                isAsync: true,
                concurrency: concurrency);
        }

        private sealed class E3DiagnosisRecord
        {
            public int ConcurrencyLevel { get; set; }
            public DiagnosisBenchmarkResult Result { get; set; } = new();
            public double WallClockMs { get; set; }
            public int ManagedThreadId { get; set; }
        }

        private static void ExportDiagnosisResults(
            IEnumerable<E3DiagnosisRecord> records,
            string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using StreamWriter writer = new(filePath, false, System.Text.Encoding.UTF8);
            var inv = CultureInfo.InvariantCulture;

            writer.WriteLine(
                "ConcurrencyLevel,Iteration,StudyId,WallClockMs,ManagedThreadId," +
                "HeartRateRisk,BloodPressureRisk,PressureLoadRisk,NocturnalRisk," +
                "DiagnosisLabel,NocturnalProfile,LoadDiagnosis,HeartRateDiagnosis," +
                "TrendMatchesStored");

            foreach (var rec in records)
            {
                var r = rec.Result;
                writer.WriteLine(string.Join(",",
                    rec.ConcurrencyLevel,
                    r.Iteration,
                    r.StudyId,
                    rec.WallClockMs.ToString("R", inv),
                    rec.ManagedThreadId,
                    r.HeartRateRisk.ToString("R", inv),
                    r.BloodPressureRisk.ToString("R", inv),
                    r.PressureLoadRisk.ToString("R", inv),
                    r.NocturnalRisk.ToString("R", inv),
                    $"\"{r.DiagnosisLabel}\"",
                    $"\"{r.NocturnalProfile}\"",
                    $"\"{r.LoadDiagnosis}\"",
                    $"\"{r.HeartRateDiagnosis}\"",
                    r.TrendMatchesStored ? "true" : "false"));
            }
        }

        private static void ExportIterationSummary(
            IEnumerable<E3IterationSummary> rows,
            string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using StreamWriter writer = new(filePath, false, System.Text.Encoding.UTF8);
            var inv = CultureInfo.InvariantCulture;

            writer.WriteLine(
                "ConcurrencyLevel,Iteration,Studies,Errors,MakespanMs," +
                "ThroughputStudiesPerSecond,StartedAtUtc");

            foreach (var x in rows)
            {
                writer.WriteLine(string.Join(",",
                    x.ConcurrencyLevel,
                    x.Iteration,
                    x.Studies,
                    x.Errors,
                    x.MakespanMs.ToString("R", inv),
                    x.ThroughputStudiesPerSecond.ToString("R", inv),
                    x.StartedAtUtc.ToString("O", inv)));
            }
        }
    }
}
