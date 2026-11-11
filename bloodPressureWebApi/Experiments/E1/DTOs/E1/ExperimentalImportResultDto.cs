namespace bloodPressureWebApi.DTOs.Experiments
{
    public class ExperimentalImportResultDto
    {
        public Guid BatchId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public int FilesFound { get; set; }

        public int Imported { get; set; }

        public int Failed { get; set; }

        public int Duplicates { get; set; }

        public List<ExperimentalStudyResultDto>
            Studies
        { get; set; } = new();
    }


    public class ExperimentalStudyResultDto
    {
        public string FileName { get; set; }
            = string.Empty;

        public string ExternalPatientCode { get; set; }
            = string.Empty;

        public int? PatientId { get; set; }

        public int? StudyId { get; set; }

        public string Status { get; set; }
            = string.Empty;

        public string? Error { get; set; }
    }
}