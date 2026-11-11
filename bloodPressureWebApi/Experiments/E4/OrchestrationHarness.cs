using bloodPressureWebApi.Data;
using bloodPressureWebApi.Experiments.E7;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace bloodPressureWebApi.Experiments.E4
{
    /// <summary>
    /// Configuracion de orquestacion evaluada por E4/E6.
    ///   Sync           : el runner llama RunDiagnosis estudio por estudio.
    ///   Async(c)       : todos los estudios se encolan; c consumidores los procesan.
    /// </summary>
    public sealed record OrchestrationConfig(string Name, bool IsAsync, int Consumers)
    {
        public static OrchestrationConfig Sync() => new("Sync", false, 1);

        public static OrchestrationConfig Async(int consumers) =>
            new($"Async_{consumers}c", true, consumers);
    }

    public sealed class JobRecord
    {
        public string Config { get; set; } = "";
        public int Iteration { get; set; }
        public int OrderInIteration { get; set; }
        public int StudyId { get; set; }
        public bool Success { get; set; }
        public int Attempts { get; set; }
        public int ConsumerIndex { get; set; } = -1;

        /// <summary>Desde el envio (encolado o llamada) hasta el resultado.</summary>
        public double LatencyMs { get; set; }

        /// <summary>Espera en cola antes del ultimo intento (0 en Sync).</summary>
        public double QueueWaitMs { get; set; }

        /// <summary>Duracion de RunDiagnosis (ultimo intento).</summary>
        public double DiagnosisMs { get; set; }

        public bool MatchesBaseline { get; set; }
        public string Error { get; set; } = "";
    }

    public sealed class BatchRecord
    {
        public string Config { get; set; } = "";
        public bool IsAsync { get; set; }
        public int Consumers { get; set; }
        public int Iteration { get; set; }
        public int PositionInIteration { get; set; }
        public int Studies { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }

        /// <summary>Tiempo de pared desde el primer envio hasta el ultimo resultado.</summary>
        public double MakespanMs { get; set; }

        public double ThroughputPerSecond { get; set; }
        public double MedianLatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public int MatchesBaseline { get; set; }
        public DateTime StartedAtUtc { get; set; }
    }

    /// <summary>
    /// Ejecuta un lote de estudios con una configuracion y lo mide.
    /// No comparte profiler entre hilos: todo se mide con marcas de tiempo.
    /// </summary>
    public sealed class OrchestrationHarness
    {
        private readonly ApplicationDbContext _context;
        private readonly AIDiagnosisService _diagnosisService;
        private readonly DiagnosisQueue _queue;
        private readonly DiagnosisWorkerOptions _workerOptions;

        public OrchestrationHarness(
            ApplicationDbContext context,
            AIDiagnosisService diagnosisService,
            DiagnosisQueue queue,
            DiagnosisWorkerOptions workerOptions)
        {
            _context = context;
            _diagnosisService = diagnosisService;
            _queue = queue;
            _workerOptions = workerOptions;
        }

        public Dictionary<int, string> Baseline { get; } = new();

        public async Task<List<int>> LoadStudyIdsAsync(List<int>? requested)
        {
            IQueryable<MAPAStudy> q = _context.MAPAStudies.AsNoTracking()
                .Where(s => s.Features != null);

            if (requested != null && requested.Count > 0)
            {
                q = q.Where(s => requested.Contains(s.StudyId));
            }

            var ids = await q.OrderBy(s => s.StudyId).Select(s => s.StudyId).ToListAsync();

            if (ids.Count == 0)
            {
                throw new InvalidOperationException("No hay estudios con features.");
            }

            return ids;
        }

        /// <summary>
        /// Pasada secuencial NO medida: calienta el sistema y define la linea
        /// base de resultados (hash por estudio).
        /// </summary>
        public async Task WarmUpAndBaselineAsync(List<int> studyIds)
        {
            foreach (int id in studyIds)
            {
                await _diagnosisService.RunDiagnosis(id, includeFramingham: false);
                Baseline[id] = await OutputHashAsync(id);
                _context.ChangeTracker.Clear();
            }
        }

        public async Task<(BatchRecord Batch, List<JobRecord> Jobs)> RunBatchAsync(
            OrchestrationConfig config, List<int> studyIds, int iteration, int positionInIteration)
        {
            var jobs = new List<JobRecord>();
            DateTime startedUtc = DateTime.UtcNow;
            var batchTimer = Stopwatch.StartNew();

            if (!config.IsAsync)
            {
                int order = 0;
                foreach (int id in studyIds)
                {
                    order++;
                    var rec = new JobRecord
                    {
                        Config = config.Name, Iteration = iteration,
                        OrderInIteration = order, StudyId = id, Attempts = 1
                    };

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        await _diagnosisService.RunDiagnosis(id, includeFramingham: false);
                        sw.Stop();
                        rec.Success = true;
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        rec.Error = ex.Message;
                    }
                    finally
                    {
                        _context.ChangeTracker.Clear();
                    }

                    rec.DiagnosisMs = sw.Elapsed.TotalMilliseconds;
                    rec.LatencyMs = rec.DiagnosisMs;
                    jobs.Add(rec);
                }

                batchTimer.Stop();
            }
            else
            {
                _workerOptions.ActiveConsumers = config.Consumers;

                var pending = new List<(DiagnosisJob Job, int Order)>();
                int order = 0;

                foreach (int id in studyIds)
                {
                    order++;
                    var job = new DiagnosisJob
                    {
                        StudyId = id,
                        IncludeFramingham = false,
                        CompletionSource = new TaskCompletionSource<bool>(
                            TaskCreationOptions.RunContinuationsAsynchronously)
                    };
                    await _queue.EnqueueAsync(job);
                    pending.Add((job, order));
                }

                // Esperar todos (los fallidos tambien terminan)
                foreach (var (job, _) in pending)
                {
                    try { await job.CompletionSource!.Task; } catch { /* se registra abajo */ }
                }

                batchTimer.Stop();

                foreach (var (job, ord) in pending)
                {
                    bool ok = job.CompletionSource!.Task.IsCompletedSuccessfully;
                    jobs.Add(new JobRecord
                    {
                        Config = config.Name,
                        Iteration = iteration,
                        OrderInIteration = ord,
                        StudyId = job.StudyId,
                        Success = ok,
                        Attempts = job.RetryCount + 1,
                        ConsumerIndex = job.ConsumerIndex,
                        LatencyMs = DiagnosisJob.ElapsedMs(job.FirstEnqueuedTimestamp, job.CompletedTimestamp),
                        QueueWaitMs = DiagnosisJob.ElapsedMs(job.FirstEnqueuedTimestamp, job.WorkerStartedTimestamp),
                        DiagnosisMs = DiagnosisJob.ElapsedMs(job.DiagnosisStartedTimestamp, job.DiagnosisCompletedTimestamp),
                        Error = ok ? "" : (job.LastError ?? "")
                    });
                }
            }

            // Integridad: resultado == linea base
            foreach (var j in jobs.Where(j => j.Success))
            {
                j.MatchesBaseline = Baseline.TryGetValue(j.StudyId, out var b) &&
                                    b == await OutputHashAsync(j.StudyId);
            }
            _context.ChangeTracker.Clear();

            double makespan = batchTimer.Elapsed.TotalMilliseconds;
            var lat = jobs.Where(j => j.Success).Select(j => j.LatencyMs).OrderBy(x => x).ToList();

            var batch = new BatchRecord
            {
                Config = config.Name,
                IsAsync = config.IsAsync,
                Consumers = config.Consumers,
                Iteration = iteration,
                PositionInIteration = positionInIteration,
                Studies = jobs.Count,
                Successful = jobs.Count(j => j.Success),
                Failed = jobs.Count(j => !j.Success),
                MakespanMs = makespan,
                ThroughputPerSecond = makespan > 0 ? jobs.Count(j => j.Success) / (makespan / 1000.0) : 0,
                MedianLatencyMs = Percentile(lat, 50),
                P95LatencyMs = Percentile(lat, 95),
                MatchesBaseline = jobs.Count(j => j.MatchesBaseline),
                StartedAtUtc = startedUtc
            };

            return (batch, jobs);
        }

        public void ResetWorker() => _workerOptions.ActiveConsumers = 1;

        private async Task<string> OutputHashAsync(int studyId)
        {
            var d = await _context.AIDiagnosis.AsNoTracking().FirstAsync(x => x.StudyId == studyId);
            return ResultSignatureBuilder.ComputeHash(ResultSignatureBuilder.BuildOutputSignature(d));
        }

        private static double Percentile(List<double> sorted, double q)
        {
            if (sorted.Count == 0) return double.NaN;
            double pos = (sorted.Count - 1) * q / 100.0;
            int lo = (int)Math.Floor(pos), hi = (int)Math.Ceiling(pos);
            return sorted[lo] + (pos - lo) * (sorted[hi] - sorted[lo]);
        }

        // =====================================================
        // EXPORT
        // =====================================================

        private static string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        public static void ExportBatches(IEnumerable<BatchRecord> rows, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
            var inv = CultureInfo.InvariantCulture;
            w.WriteLine("Config,IsAsync,Consumers,Iteration,PositionInIteration,Studies,Successful,Failed," +
                        "MakespanMs,ThroughputPerSecond,MedianLatencyMs,P95LatencyMs,MatchesBaseline,StartedAtUtc");
            foreach (var b in rows)
            {
                w.WriteLine(string.Join(",", Q(b.Config), b.IsAsync ? "true" : "false", b.Consumers,
                    b.Iteration, b.PositionInIteration, b.Studies, b.Successful, b.Failed,
                    b.MakespanMs.ToString("R", inv), b.ThroughputPerSecond.ToString("R", inv),
                    b.MedianLatencyMs.ToString("R", inv), b.P95LatencyMs.ToString("R", inv),
                    b.MatchesBaseline, Q(b.StartedAtUtc.ToString("O", inv))));
            }
        }

        public static void ExportJobs(IEnumerable<JobRecord> rows, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
            var inv = CultureInfo.InvariantCulture;
            w.WriteLine("Config,Iteration,OrderInIteration,StudyId,Success,Attempts,ConsumerIndex," +
                        "LatencyMs,QueueWaitMs,DiagnosisMs,MatchesBaseline,Error");
            foreach (var j in rows)
            {
                w.WriteLine(string.Join(",", Q(j.Config), j.Iteration, j.OrderInIteration, j.StudyId,
                    j.Success ? "true" : "false", j.Attempts, j.ConsumerIndex,
                    j.LatencyMs.ToString("R", inv), j.QueueWaitMs.ToString("R", inv),
                    j.DiagnosisMs.ToString("R", inv), j.MatchesBaseline ? "true" : "false", Q(j.Error)));
            }
        }
    }
}
