namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class QuickDiagnosisResponseDto
    {
        public double AverageSystolic { get; set; }

        public double AverageDiastolic { get; set; }

        public double AveragePulse { get; set; }

        public double FuzzyHypertensionRisk { get; set; }

        public double HeartRateRisk { get; set; }

        public double HeartAge { get; set; }

        public double FuzzyCardiovascularRisk { get; set; }
    }
}