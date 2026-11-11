using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Services;

using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace bloodPressureWebApi.Workers
{
    /// <summary>
    /// Consumidor de la cola de diagnostico.
    ///
    /// CAMBIOS RESPECTO A LA VERSION ANTERIOR
    ///  1. Varios consumidores (DiagnosisWorkerOptions.ActiveConsumers; por
    ///     defecto 1 = comportamiento original). Antes habia UN solo ciclo de
    ///     consumo: el modo "asincrono" procesaba en serie.
    ///  2. BUG "49 de 50": el 'using var stage' vivia hasta el final del bloque
    ///     try, es decir, DESPUES de TrySetResult. El experimento despertaba y
    ///     leia las metricas antes de que la ultima se registrara. Ahora la
    ///     medicion cierra antes de completar el job.
    ///  3. BUG cuelgue: si el estudio no existia se hacia 'continue' y el job
    ///     nunca se completaba (el experimento esperaba para siempre). Ahora
    ///     se completa con excepcion.
    ///  4. BUG cuelgue: en el fallo definitivo, si guardar el estado fallaba,
    ///     el 'catch {}' vacio impedia completar el job. Ahora SIEMPRE se
    ///     completa.
    ///  5. Marcas de tiempo de alta resolucion: espera en cola, diagnostico y
    ///     tiempo total, por intento.
    /// </summary>
    public class DiagnosisWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DiagnosisQueue _queue;
        private readonly DiagnosisWorkerOptions _options;

        public DiagnosisWorker(
            IServiceScopeFactory scopeFactory,
            DiagnosisQueue queue,
            DiagnosisWorkerOptions options)
        {
            _scopeFactory = scopeFactory;
            _queue = queue;
            _options = options;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var loops = Enumerable.Range(0, _options.MaxConsumers)
                .Select(i => Task.Run(() => ConsumeLoopAsync(i, stoppingToken), stoppingToken))
                .ToArray();

            return Task.WhenAll(loops);
        }

        private async Task ConsumeLoopAsync(int index, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Consumidores inactivos esperan sin tomar trabajos
                if (index >= _options.ActiveConsumers)
                {
                    try { await Task.Delay(25, stoppingToken); }
                    catch (OperationCanceledException) { return; }
                    continue;
                }

                // Esperar a que haya trabajo SIN tomarlo; al despertar se vuelve
                // a revisar si este consumidor sigue activo (si se redujo el
                // numero de consumidores mientras esperaba, no debe consumir).
                try
                {
                    if (!await _queue.WaitToReadAsync(stoppingToken))
                    {
                        return; // cola cerrada
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (index >= _options.ActiveConsumers)
                {
                    continue;
                }

                if (_queue.TryDequeue(out DiagnosisJob job))
                {
                    await ProcessAsync(job, index);
                }
            }
        }

        private async Task ProcessAsync(DiagnosisJob job, int index)
        {
            job.WorkerStartedAt = DateTime.UtcNow;
            job.WorkerStartedTimestamp = Stopwatch.GetTimestamp();
            job.ConsumerIndex = index;

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var diagnosisService = scope.ServiceProvider.GetRequiredService<AIDiagnosisService>();

                var study = await context.MAPAStudies
                    .FirstOrDefaultAsync(x => x.StudyId == job.StudyId);

                if (study == null)
                {
                    // CORRECCION: antes 'continue' -> el job nunca terminaba
                    throw new InvalidOperationException($"Study {job.StudyId} not found.");
                }

                // Estados de procesamiento (comportamiento original)
                study.ProcessingStatus = StudyProcessingStatus.Processing;
                await context.SaveChangesAsync();

                study.ProcessingStatus = StudyProcessingStatus.FeatureExtraction;
                await context.SaveChangesAsync();

                study.ProcessingStatus = StudyProcessingStatus.AIAnalysis;
                await context.SaveChangesAsync();

                // ----- DIAGNOSTICO (medicion cerrada ANTES de completar el job) -----
                job.DiagnosisStartedTimestamp = Stopwatch.GetTimestamp();

                if (job.Profiler != null)
                {
                    using (var stage = job.Profiler.Stage("Diagnosis"))
                    {
                        try
                        {
                            await diagnosisService.RunDiagnosis(job.StudyId, job.IncludeFramingham);
                        }
                        catch (Exception ex)
                        {
                            stage.Fail(ex);
                            throw;
                        }
                    }
                }
                else
                {
                    await diagnosisService.RunDiagnosis(job.StudyId, job.IncludeFramingham);
                }

                job.DiagnosisCompletedTimestamp = Stopwatch.GetTimestamp();

                study.ProcessingStatus = StudyProcessingStatus.Completed;
                study.StudyStatus = "ANALYZED";
                await context.SaveChangesAsync();

                job.CompletedAt = DateTime.UtcNow;
                job.CompletedTimestamp = Stopwatch.GetTimestamp();
                job.LastError = null;

                job.CompletionSource?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                job.LastError = ex.Message;
                Console.WriteLine($"WORKER[{index}] ERROR study {job.StudyId}: {ex.Message}");

                if (job.RetryCount < _options.MaxRetries)
                {
                    job.RetryCount++;
                    Console.WriteLine($"WORKER[{index}] RETRY {job.RetryCount} study {job.StudyId}");
                    await _queue.EnqueueAsync(job);
                    return;
                }

                // Fallo definitivo
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var study = await context.MAPAStudies
                        .FirstOrDefaultAsync(x => x.StudyId == job.StudyId);

                    if (study != null)
                    {
                        study.ProcessingStatus = StudyProcessingStatus.Failed;
                        study.ProcessingError = ex.Message;
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"WORKER[{index}] could not save failure state: {saveEx.Message}");
                }
                finally
                {
                    // CORRECCION: el job SIEMPRE se completa
                    job.CompletedAt = DateTime.UtcNow;
                    job.CompletedTimestamp = Stopwatch.GetTimestamp();
                    job.CompletionSource?.TrySetException(ex);
                }
            }
        }
    }
}
