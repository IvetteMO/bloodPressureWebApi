using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bloodPressureWebApi.Models
{
    public class AIDiagnosis
    {
        [Key]
        public int DiagnosisId { get; set; }

        [ForeignKey("Study")]
        public int StudyId { get; set; }

        // ====================================
        // FUZZY
        // ====================================


        public double? FuzzyBPRisk { get; set; } //Salida del difuso BP

        public double? FuzzyLoadC { get; set; } //Salida del difuso Carga

        public double? FuzzyNocturnalC { get; set; } //Salida del difuso Nocturnal

        public string? NocturnalProfile { get; set; } //Traducción

        public double? FuzzyHeartRC { get; set; } //Salida del difuso Heart Rate

        public string? LoadDiagnosis { get; set; } //Traducción

        public string? HeartRateDiagnosis { get; set; } //Traducción

        // ====================================
        // Neural Network
        // ====================================

        public double? Hypertension4YearRisk { get; set; }


        public double? CardiovascularRisk { get; set; }

        // ====================================
        // FINAL
        // ====================================

        public string? FinalRiskLevel { get; set; } //Traducción del Clasificador BP

        public double? HeartAge { get; set; }

        public string? Explanations { get; set; }


        // ====================================
        // TRACEABILITY
        // ====================================

        public string? ModelVersion { get; set; }

        public string? MatlabRuntimeVersion { get; set; }

        public string? FeaturePipelineVersion { get; set; }

        public double? ConfidenceScore { get; set; }


        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // ====================================
        // NAVIGATION
        // ====================================

        public MAPAStudy? Study { get; set; }
    }
}