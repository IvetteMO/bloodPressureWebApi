using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Workers
{
    /// <summary>
    /// Al arrancar, vuelve a encolar los estudios que quedaron a medio
    /// recalcular.
    ///
    /// La cola de diagnóstico vive en memoria: si el backend se reinicia
    /// mientras un estudio está QUEUED o PROCESSING, ese job se pierde y
    /// el estudio se quedaría "Recalculando…" para siempre (el frontend
    /// seguiría esperando). Aquí se recuperan.
    ///
    /// No toca estudios UPLOADED, COMPLETED ni FAILED.
    /// </summary>
    public sealed class DiagnosisRecoveryService(
        IServiceScopeFactory scopeFactory,
        DiagnosisQueue queue,
        ILogger<DiagnosisRecoveryService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var inProgress = StudyProcessingStatus.InProgress.ToList();

                var stuckStudyIds = await context.MAPAStudies
                    .AsNoTracking()
                    .Where(x => x.ProcessingStatus != null
                        && inProgress.Contains(x.ProcessingStatus))
                    .Select(x => x.StudyId)
                    .ToListAsync(stoppingToken);

                if (stuckStudyIds.Count == 0)
                {
                    return;
                }

                logger.LogWarning(
                    "Re-queuing {Count} studies left in progress by a previous run: {StudyIds}",
                    stuckStudyIds.Count,
                    string.Join(", ", stuckStudyIds));

                foreach (var studyId in stuckStudyIds)
                {
                    await queue.EnqueueAsync(new DiagnosisJob { StudyId = studyId });
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // El host se está apagando.
            }
            catch (Exception ex)
            {
                // Si falla la recuperación, la API debe seguir funcionando.
                logger.LogError(ex, "Could not recover in-progress diagnoses at startup.");
            }
        }
    }
}
