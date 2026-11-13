using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace bloodPressureWebApi.Infrastructure.Matlab
{
    /// <summary>
    /// /health/ready: Healthy cuando el MATLAB Runtime ya está inicializado.
    /// Un balanceador o Azure Container Apps puede esperar este estado
    /// antes de mandar tráfico a una instancia nueva.
    /// </summary>
    public sealed class MatlabHealthCheck(
        MatlabReadiness readiness,
        IConfiguration configuration) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (readiness.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"MATLAB Runtime initialized in {readiness.InitializationMs:F0} ms."));
            }

            if (readiness.Error is not null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "MATLAB Runtime failed to initialize."));
            }

            // Sin calentamiento (Development / experimentos): se inicializa
            // con la primera petición, así que la API sí puede recibir tráfico.
            if (!configuration.GetValue("Matlab:WarmupOnStartup", true))
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    "MATLAB Runtime initializes on first use (warmup disabled)."));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(
                "MATLAB Runtime is still initializing."));
        }
    }
}
