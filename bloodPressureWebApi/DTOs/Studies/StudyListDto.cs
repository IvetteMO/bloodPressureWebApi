namespace bloodPressureWebApi.DTOs.Studies
{
    public class StudyListDto
    {
        public int StudyId { get; set; }

        public string? ExcelFileName { get; set; }

        public int? TotalReadings { get; set; }

        public DateTime StudyDate { get; set; }

        public string? StudyStatus { get; set; }
    }
}
