using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bloodPressureWebApi.Models
{
    public class MAPAStudy
    {
        [Key]
        public int StudyId { get; set; }

        [ForeignKey("Patient")]
        public int PatientId { get; set; }

        // ========================================
        // IMPORT
        // ========================================

        public string? ExternalStudyCode { get; set; }

        public string? ImportHash { get; set; }

        public int Version { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public int? PreviousStudyId { get; set; }

        // ========================================
        // STUDY
        // ========================================

        public DateTime StudyDate { get; set; }

        public TimeSpan? AwakeTime { get; set; }

        public TimeSpan? SleepTime { get; set; }

        public Int32? Age { get; set; }

        public bool CustomSchedule { get; set; }

        public string? ExcelFileName { get; set; }

        public int? TotalReadings { get; set; }

        public double? CorrectReadingsPercent { get; set; }

        // ========================================
        // STATUS
        // ========================================

        public string? StudyStatus { get; set; }

        public string? ProcessingStatus { get; set; }

        public string? ProcessingError { get; set; }

        // ========================================
        // COMMENTS
        // ========================================

        public string? Comments { get; set; }

        // ========================================
        // AUDIT
        // ========================================

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // ========================================
        // NAVIGATION
        // ========================================

        public Patients? Patient { get; set; }

        public ICollection<MAPAReading>
            Readings
        { get; set; }
            =
            new List<MAPAReading>();

        public ICollection<PatientRiskFactor>
            RiskFactors
        { get; set; }
            =
            new List<PatientRiskFactor>();

        public MAPAFeature? Features { get; set; }

        public AIDiagnosis? Diagnosis { get; set; }
    }
}
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace bloodPressureWebApi.Models
//{
//    public class MAPAStudy
//    {
//        [Key]
//        public int StudyId { get; set; }

//        [ForeignKey("Patient")]
//        public int PatientId { get; set; }

//        public string? ExternalStudyCode { get; set; }

//        public string? ImportHash { get; set; }

//        public DateTime StudyDate { get; set; }

//        public TimeSpan? AwakeTime { get; set; }

//        public TimeSpan? SleepTime { get; set; }

//        public bool CustomSchedule { get; set; }

//        public string? ExcelFileName { get; set; }

//        public int? TotalReadings { get; set; }

//        public double? CorrectReadingsPercent { get; set; }

//        public string? StudyStatus { get; set; }

//        public string? Comments { get; set; }

//        public DateTime CreatedAt { get; set; }
//            = DateTime.Now;

//        // ========================================
//        // NAVIGATION
//        // ========================================

//        public Patients? Patient { get; set; }

//        public ICollection<MAPAReading>
//            Readings
//        { get; set; }
//            =
//            new List<MAPAReading>();

//        public ICollection<PatientRiskFactor>
//            RiskFactors
//        { get; set; }
//            =
//            new List<PatientRiskFactor>();

//        public MAPAFeature? Features { get; set; }

//        public AIDiagnosis? Diagnosis { get; set; }
//    }
//}