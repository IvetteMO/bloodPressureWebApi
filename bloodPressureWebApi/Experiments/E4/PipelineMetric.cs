namespace bloodPressureWebApi.Experiments.E4
{
    public class PipelineMetric
    {
        public int StudyId { get; set; }

        public string Mode { get; set; } = "";

        public double ResponseTimeMs { get; set; }

        public double ProcessingTimeMs { get; set; }

        public double TotalTimeMs { get; set; }

        public bool Success { get; set; }

        public string? Error { get; set; }
    }
}