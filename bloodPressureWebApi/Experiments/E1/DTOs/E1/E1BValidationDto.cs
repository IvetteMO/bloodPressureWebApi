namespace bloodPressureWebApi.DTOs.E1
{
    public class E1BValidationDto
    {
        public Guid BatchId { get; set; }

        public int StudyId { get; set; }

        public string ExternalPatientCode { get; set; }
            = string.Empty;

        public string SourceFileName { get; set; }
            = string.Empty;

        public bool FinalDiagnosisMatch { get; set; }

        public bool NocturnalProfileMatch { get; set; }

        public double MeanAbsoluteError { get; set; }

        public double MaximumAbsoluteError { get; set; }
    }
}