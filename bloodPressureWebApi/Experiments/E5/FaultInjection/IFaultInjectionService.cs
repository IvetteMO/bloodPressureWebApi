namespace bloodPressureWebApi.Experiments.E5.FaultInjection
{
    public interface IFaultInjectionService
    {
        FaultInjectionOptions Options { get; }

        void Configure(
            FaultScenario scenario,
            int iteration);

        void Disable();

        void ThrowIfRequired();
    }
}