using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Experiments.E4;
using bloodPressureWebApi.Experiments.E5.FaultInjection;
using System.Diagnostics;

namespace bloodPressureWebApi.Experiments.E6
{
    /// <summary>
    /// E6 - Configuracion arquitectonica: orquestacion x numero de consumidores.
    ///
    /// REDISENO
    ///   La version anterior tenia 8 configuraciones, pero "Cold"/"Warm" era
    ///   SOLO una etiqueta (ejecutaban el mismo codigo): en realidad eran 4
    ///   configuraciones duplicadas, lo que invalida la comparacion de 8 grupos.
    ///   Tampoco habia paralelismo real: la cola tenia un solo consumidor, y
    ///   "10/25 usuarios" solo cambiaba el tamano del lote encolado.
    ///   El arranque en frio se caracteriza en E2 (no se puede forzar dentro
    ///   del mismo proceso).
    ///
    ///   Ahora cada configuracion es un tratamiento real:
    ///     Sync, Async_1c, Async_2c, Async_4c, ... (consumidores de la cola)
    ///   Unidad experimental: el lote completo (50 estudios) por iteracion.
    ///   Respuesta: makespan; tambien throughput y latencias por job.
    ///   El orden de las configuraciones ROTA en cada iteracion (cuadrado
    ///   latino) para que ninguna quede siempre primera o ultima.
    /// </summary>
    public class E6ExperimentRunner
    {
        private readonly OrchestrationHarness _harness;
        private readonly IFaultInjectionService _faultInjection;

        public E6ExperimentRunner(
            OrchestrationHarness harness,
            IFaultInjectionService faultInjection)
        {
            _harness = harness;
            _faultInjection = faultInjection;
        }

        public async Task<E6ExperimentResultDto> RunAsync(E6ExperimentRequestDto request)
        {
            var result = new E6ExperimentResultDto { StartedAt = DateTime.UtcNow };
            var total = Stopwatch.StartNew();

            _faultInjection.Disable();

            var studyIds = await _harness.LoadStudyIdsAsync(request.StudyIds);
            result.StudiesProcessed = studyIds.Count;
            result.Iterations = request.Iterations;

            // Configuraciones: Sync + Async con cada numero de consumidores
            var configs = new List<OrchestrationConfig>();
            if (request.IncludeSync) configs.Add(OrchestrationConfig.Sync());

            var consumers = (request.AsyncConsumers is { Count: > 0 }
                    ? request.AsyncConsumers
                    : new List<int> { 1, 2, 4 })
                .Where(c => c > 0).Distinct().OrderBy(c => c);

            foreach (int c in consumers) configs.Add(OrchestrationConfig.Async(c));

            result.Configurations = configs.Select(c => c.Name).ToList();

            var batches = new List<BatchRecord>();
            var jobs = new List<JobRecord>();

            try
            {
                var warm = Stopwatch.StartNew();
                await _harness.WarmUpAndBaselineAsync(studyIds);
                result.WarmUpSeconds = warm.Elapsed.TotalSeconds;

                for (int it = 1; it <= request.Iterations; it++)
                {
                    // Rotacion del orden (cuadrado latino)
                    int shift = (it - 1) % configs.Count;
                    var order = configs.Skip(shift).Concat(configs.Take(shift)).ToList();

                    for (int p = 0; p < order.Count; p++)
                    {
                        var (batch, batchJobs) = await _harness.RunBatchAsync(order[p], studyIds, it, p + 1);
                        batches.Add(batch);
                        jobs.AddRange(batchJobs);
                    }
                }
            }
            finally
            {
                _harness.ResetWorker();
            }

            result.Batches = batches;
            result.TotalJobs = jobs.Count;
            result.TotalFailures = jobs.Count(j => !j.Success);
            result.ResultsMatchingBaseline = jobs.Count(j => j.MatchesBaseline);

            string folder = ExperimentPaths.CreateOutputFolder("E6");
            result.OutputFolder = folder;

            if (request.ExportCsv)
            {
                OrchestrationHarness.ExportBatches(batches, Path.Combine(folder, "BatchSummary.csv"));
                OrchestrationHarness.ExportJobs(jobs, Path.Combine(folder, "JobRecords.csv"));
            }

            total.Stop();
            result.TotalExecutionTimeSeconds = total.Elapsed.TotalSeconds;
            result.FinishedAt = DateTime.UtcNow;
            return result;
        }
    }
}
