namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class HyperRiskResponseDto
    {
        public double HypertensionRisk { get; set; }

        public int PredictedSystolic { get; set; }

        public int PredictedDiastolic { get; set; }
    }
}
