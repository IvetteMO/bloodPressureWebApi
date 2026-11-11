
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bloodPressureWebApi.Models
{
    public class PatientRiskFactor
    {
        [Key]
        public int RiskFactorId { get; set; }

        [ForeignKey("Study")]
        public int StudyId { get; set; }

        // ====================================
        // BODY
        // ====================================

        public double? Weight { get; set; }

        public double? Height { get; set; }

        public double? BMI { get; set; }

        // ====================================
        // LIFESTYLE
        // ====================================

        public byte? Smoking { get; set; }

        // ====================================
        // CLINICAL
        // ====================================

        public byte? Diabetes { get; set; }

        public byte? HypertensionTreatment { get; set; }

        // ====================================
        // FAMILY HISTORY
        // ====================================

        // 0 = none
        // 1 = one parent
        // 2 = both parents

        public byte? ParentalHypertension { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // ====================================
        // NAVIGATION
        // ====================================

        public MAPAStudy? Study { get; set; }
    }
}