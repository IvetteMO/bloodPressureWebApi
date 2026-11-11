namespace bloodPressureWebApi.DTOs.Import
{
    public class ImportPreviewDto
    {
        public bool StudyExists { get; set; }

        public bool SameHash { get; set; }

        public int? ExistingStudyId { get; set; }

        public string? ExistingFileName { get; set; }

        public DateTime? ExistingStudyDate { get; set; }

        public int IncomingReadings { get; set; }

        public string RecommendedAction { get; set; }
            = "CREATE_NEW";
    }
}