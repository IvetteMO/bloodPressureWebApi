using bloodPressureWebApi.Experiments.E4;

namespace bloodPressureWebApi.Experiments.E6
{
    public class E6ExperimentResultDto
    {
        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public int StudiesProcessed { get; set; }

        public int Iterations { get; set; }

        public List<string> Configurations { get; set; } = new();

        public string OutputFolder { get; set; } = string.Empty;

        public double WarmUpSeconds { get; set; }

        public int TotalJobs { get; set; }

        public int TotalFailures { get; set; }

        public int ResultsMatchingBaseline { get; set; }

        public List<BatchRecord> Batches { get; set; } = new();

        public double TotalExecutionTimeSeconds { get; set; }
    }
}
