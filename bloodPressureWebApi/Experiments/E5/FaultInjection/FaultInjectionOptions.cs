namespace bloodPressureWebApi.Experiments.E5.FaultInjection
{
    public class FaultInjectionOptions
    {
        public bool Enabled { get; set; }

        public FaultScenario Scenario { get; set; }

        public int CurrentIteration { get; set; }

        public int CurrentStudy { get; set; }
    }
}