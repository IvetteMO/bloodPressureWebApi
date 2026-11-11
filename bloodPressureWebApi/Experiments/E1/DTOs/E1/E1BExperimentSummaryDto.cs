namespace bloodPressureWebApi.DTOs.E1
{
    public class E1BExperimentSummaryDto
    {
        public Guid BatchId { get; set; }

        public int Studies { get; set; }

        public int ExactDiagnosisMatches { get; set; }

        public double ExactDiagnosisAgreement { get; set; }

        public int ExactNocturnalMatches { get; set; }

        public double ExactNocturnalAgreement { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }
    }
}