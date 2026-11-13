using bloodPressureWebApi.Services;

namespace bloodPressureWebApi.Infrastructure.Matlab
{
    /// <summary>
    /// Inicializa el MATLAB Runtime al arrancar el backend, en segundo plano.
    ///
    /// Sin esto, el MatlabService (singleton) se crea la primera vez que
    /// alguien lo necesita, y ese primer usuario espera ~20 s después de
    /// cada reinicio. Aquí se paga ese costo al arrancar: la API ya
    /// responde mientras tanto, y /health/ready indica cuándo terminó.
    ///
    /// Se controla con Matlab:WarmupOnStartup. En Development está en false
    /// para que E2/E3 sigan midiendo el arranque en frío igual que en el
    /// artículo (su primera iteración es la que inicializa el Runtime).
    /// </summary>
    public sealed class MatlabWarmupService(
        IServiceProvider services,
        MatlabReadiness readiness,
        ILogger<MatlabWarmupService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // El constructor de MatlabService es síncrono y tarda ~20 s:
            // se ejecuta fuera del hilo de arranque para no retrasar la API.
            await Task.Yield();

            try
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();

                services.GetRequiredService<MatlabService>();

                timer.Stop();

                readiness.MarkReady(timer.Elapsed.TotalMilliseconds);

                logger.LogInformation(
                    "MATLAB Runtime ready in {Milliseconds:F0} ms.",
                    timer.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                readiness.MarkFailed(ex.Message);

                logger.LogError(ex, "MATLAB Runtime could not be initialized.");
            }
        }
    }
}
