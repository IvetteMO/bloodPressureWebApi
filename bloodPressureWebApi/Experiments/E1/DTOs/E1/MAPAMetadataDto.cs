namespace bloodPressureWebApi.DTOs.Experiments
{
    public class MAPAMetadataDto
    {
        // ==========================================
        // IDENTIFICATION
        // ==========================================

        public string ExternalPatientCode { get; set; }
            = string.Empty;

        public string SourceFileName { get; set; }
            = string.Empty;


        // ==========================================
        // ORIGINAL 24H VALUES
        // ==========================================

        public double? Original24hSystolicMean { get; set; }

        public double? Original24hDiastolicMean { get; set; }

        public double? OriginalMeanPulse { get; set; }


        // ==========================================
        // ORIGINAL DAY VALUES
        // ==========================================

        public double? OriginalDaySystolicMean { get; set; }

        public double? OriginalDayDiastolicMean { get; set; }

        public double? OriginalDayPulseMean { get; set; }


        // ==========================================
        // ORIGINAL NIGHT VALUES
        // ==========================================

        public double? OriginalNightSystolicMean { get; set; }

        public double? OriginalNightDiastolicMean { get; set; }

        public double? OriginalNightPulseMean { get; set; }


        // ==========================================
        // PRESSURE LOAD
        // ==========================================

        public double? OriginalDayPressureLoad { get; set; }

        public double? OriginalNightPressureLoad { get; set; }


        // ==========================================
        // DIPPING
        // ==========================================

        public double? OriginalSystolicDippingPercent { get; set; }

        public double? OriginalDiastolicDippingPercent { get; set; }
    }
}