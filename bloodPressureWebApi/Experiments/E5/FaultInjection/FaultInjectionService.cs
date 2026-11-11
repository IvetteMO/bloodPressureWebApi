using System;

namespace bloodPressureWebApi.Experiments.E5.FaultInjection
{
    public class FaultInjectionService : IFaultInjectionService
    {
        public FaultInjectionOptions Options { get; }
            = new();

        public void Configure(
            FaultScenario scenario,
            int iteration)
        {
            Options.Enabled = true;
            Options.Scenario = scenario;
            Options.CurrentIteration = iteration;
            Options.CurrentStudy = 0;
        }

        public void Disable()
        {
            Options.Enabled = false;
            Options.Scenario = FaultScenario.None;
        }

        public void ThrowIfRequired()
        {
            if (!Options.Enabled)
                return;

            Options.CurrentStudy++;

            switch (Options.Scenario)
            {
                case FaultScenario.None:
                    return;

                case FaultScenario.MatlabFailure:

                    throw new InvalidOperationException(
                        "Simulated AI Engine failure.");

                case FaultScenario.IntermittentFailure:

                    if (Options.CurrentStudy % 5 == 0)
                    {
                        throw new InvalidOperationException(
                            "Intermittent AI Engine failure.");
                    }

                    break;

                case FaultScenario.PersistentFailure:

                    throw new InvalidOperationException(
                        "Persistent AI Engine failure.");

                case FaultScenario.RecoveryFailure:

                    if (Options.CurrentStudy <= 10)
                    {
                        throw new InvalidOperationException(
                            "Recovery AI Engine failure.");
                    }

                    break;
            }
        }
    }
}