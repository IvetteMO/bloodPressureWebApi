namespace bloodPressureWebApi.DTOs.E1
{
    public class E1BStatisticsDto
    {
        public Guid BatchId { get; set; }

        public string ExperimentCode { get; set; }
            = "E1-B";

        public string ExperimentVersion { get; set; }
            = "E1-B_v1.0";

        public int Studies { get; set; }

        public List<E1BMetricDto> Metrics { get; set; }
            = new();
    }
}