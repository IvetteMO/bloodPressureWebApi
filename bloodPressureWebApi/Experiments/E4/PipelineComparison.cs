namespace bloodPressureWebApi.Experiments.E4
{
    public class PipelineComparison
    {
        public double AverageSyncResponseMs { get; set; }

        public double AverageAsyncResponseMs { get; set; }

        public double AverageSyncProcessingMs { get; set; }

        public double AverageAsyncProcessingMs { get; set; }

        public double AverageSyncTotalMs { get; set; }

        public double AverageAsyncTotalMs { get; set; }

        public double ResponseImprovementPercent { get; set; }

        public double ThroughputImprovementPercent { get; set; }
    }
}