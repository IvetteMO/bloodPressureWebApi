namespace bloodPressureWebApi.DTOs.Studies
{
    public class StudyDetailDto
    {
        public int StudyId { get; set; }

        public string? ExcelFileName { get; set; }

        public int? TotalReadings { get; set; }

        public DateTime StudyDate { get; set; }

        public double? Height { get; set; }

        public double? Weight { get; set; }

        public bool? Smoking { get; set; }

        public bool? Diabetes { get; set; }

        public bool? HypertensionTreatment { get; set; }

        public byte? ParentalHypertension { get; set; }

        public TimeSpan? AwakeTime { get; set; }

        public TimeSpan? SleepTime { get; set; }
    }
}
