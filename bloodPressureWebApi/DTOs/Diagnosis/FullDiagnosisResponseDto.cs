namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class FullDiagnosisResponseDto
    {
        public string BloodPressureDiagnosis
        { get; set; }

        public string HeartRateDiagnosis
        { get; set; }

        public double HypertensionRisk
        { get; set; }

        public double CvdRisk
        { get; set; }

        public double HeartAge
        { get; set; }

        public string NocturnalPattern
        { get; set; }

        public double BloodPressureTrend
        { get; set; }

        public double HeartRateTrend
        { get; set; }

        public string FinalRiskLevel
        { get; set; }
    }
}