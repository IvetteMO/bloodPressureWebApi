using System.ComponentModel.DataAnnotations;

namespace bloodPressureWebApi.Models
{
    public class Patients
    {
        [Key]
        public int PatientId { get; set; }

        public string? ExternalPatientCode { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public DateTime? BirthDate { get; set; }

        // 0 = Male
        // 1 = Female
        public byte? Sex { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // ========================================
        // NAVIGATION
        // ========================================

        public ICollection<PatientRiskFactor>
            RiskFactors
        { get; set; }
            =
            new List<PatientRiskFactor>();

        public ICollection<MAPAStudy>
            Studies
        { get; set; }
            =
            new List<MAPAStudy>();
    }
}