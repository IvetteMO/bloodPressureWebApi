using bloodPressureWebApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bloodPressureWebApi.Models
{

    public class MAPAFeature
    {
        [Key]
        public int FeatureId { get; set; }

        [ForeignKey("Study")]
        public int StudyId { get; set; }

        // ====================================
        // MEANS
        // ====================================

        public double? DaySystolicMean { get; set; }

        public double? DayDiastolicMean { get; set; }

        public double? NightSystolicMean { get; set; }

        public double? NightDiastolicMean { get; set; }

        public double? MeanPulse { get; set; }

        // ====================================
        // NOCTURNAL
        // ====================================

        public double? SystolicNightRatio { get; set; }

        public double? DiastolicNightRatio { get; set; }

        public double? DippingPercent { get; set; }

        // ====================================
        // PRESSURE LOAD
        // ====================================

        public double? DayPressureLoad { get; set; }

        public double? NightPressureLoad { get; set; }

        public double? TotalPressureLoad { get; set; }
        // ====================================
        // VARIABILITY
        // ====================================

        public double? SystolicVariability { get; set; }

        public double? DiastolicVariability { get; set; }

        public double? PulseVariability { get; set; }

        // ====================================
        // ANN TRENDS
        // ====================================

        public double? SystolicTrend { get; set; }

        public double? DiastolicTrend { get; set; }

        public double? PulseTrend { get; set; }

        // ====================================
        // QUALITY
        // ====================================

        public double? ValidReadingsPercent { get; set; }

        public int? ArtifactReadings { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public MAPAStudy? Study { get; set; }
    }
}