using bloodPressureWebApi.Experiments.E5;
using bloodPressureWebApi.Experiments.E5.FaultInjection;

namespace bloodPressureWebApi.Infrastructure.Experiments
{

    /// <summary>
    /// Implementación que se registra cuando los experimentos están
    /// deshabilitados: AIDiagnosisService la sigue llamando, pero nunca
    /// inyecta fallos y no se puede activar.
    /// </summary>
    public sealed class DisabledFaultInjectionService : IFaultInjectionService
    {
        public FaultInjectionOptions Options { get; } = new()
        {
            Enabled = false,
            Scenario = FaultScenario.None
        };

        public void Configure(FaultScenario scenario, int iteration) =>
            throw new InvalidOperationException(
                "La inyección de fallos solo está disponible con Experiments:Enabled = true.");

        public void Disable()
        {
        }

        public void ThrowIfRequired()
        {
        }
    }
}
