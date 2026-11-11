using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Experiments.E5.FaultInjection;
using bloodPressureWebApi.Services;
using System.Diagnostics;

namespace bloodPressureWebApi.Experiments.E4
{
    /// <summary>
    /// E4 - Orquestacion sincrona vs asincrona (configuracion de produccion).
    ///
    /// CAMBIOS RESPECTO A LA VERSION ANTERIOR
    ///  1. Medida principal = tiempo de pared del lote (makespan) por iteracion,
    ///     medido directamente (antes se reconstruia desde timestamps).
    ///  2. Orden alternado (ABBA): iteraciones impares Sync->Async, pares
    ///     Async->Sync. Antes Sync corria SIEMPRE primero (sesgo de orden).
    ///  3. Sin profiler compartido con el worker: se elimina el bug de las
    ///     "49 observaciones de 50".
    ///  4. Async = cola con 1 consumidor (el valor de produccion). E6 varia el
    ///     numero de consumidores.
    ///  5. Se elimina PipelineComparison: sus campos Response/Processing/Total
    ///     eran el mismo numero y la "mejora de throughput" se calculaba con
    ///     medias. Las comparaciones se hacen en MATLAB desde BatchSummary.csv.
    ///  6. Verifica que cada resultado sea igual a la linea base.
    ///  7. Inyector de fallos apagado al empezar (proteccion contra E5).
    /// </summary>
    public class E4ExperimentRunner
    {
        private readonly OrchestrationHarness _harness;
        private readonly IFaultInjectionService _faultInjection;

        public E4ExperimentRunner(
            OrchestrationHarness harness,
            IFaultInjectionService faultInjection)
        {
            _harness = harness;
            _faultInjection = faultInjection;
        }

        public async Task<E4ExperimentResultDto> RunAsync(
            E4ExperimentRequestDto request,
            CancellationToken token = default)
        {
            var result = new E4ExperimentResultDto { StartedAt = DateTime.UtcNow };
            var total = Stopwatch.StartNew();

            _faultInjection.Disable();

            var studyIds = await _harness.LoadStudyIdsAsync(request.StudyIds);
            result.StudiesProcessed = studyIds.Count;
            result.Iterations = request.Iterations;

            var sync = OrchestrationConfig.Sync();
            var async1 = OrchestrationConfig.Async(Math.Max(1, request.AsyncConsumers));

            var batches = new List<BatchRecord>();
            var jobs = new List<JobRecord>();

            try
            {
                var warm = Stopwatch.StartNew();
                await _harness.WarmUpAndBaselineAsync(studyIds);
                result.WarmUpSeconds = warm.Elapsed.TotalSeconds;

                for (int it = 1; it <= request.Iterations; it++)
                {
                    token.ThrowIfCancellationRequested();

                    var order = it % 2 == 1
                        ? new[] { sync, async1 }
                        : new[] { async1, sync };

                    for (int p = 0; p < order.Length; p++)
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
            result.TotalFailures = jobs.Count(j => !j.Success);
            result.ResultsMatchingBaseline = jobs.Count(j => j.MatchesBaseline);
            result.TotalJobs = jobs.Count;

            string folder = ExperimentPaths.CreateOutputFolder("E4");
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
