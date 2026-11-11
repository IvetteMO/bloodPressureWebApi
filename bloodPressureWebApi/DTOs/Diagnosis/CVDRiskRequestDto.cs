namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class CVDRiskRequestDto
    {
        public double Sex { get; set; }

        public double Age { get; set; }

        public double Systolic { get; set; }

        public double Smoker { get; set; }

        public double Diabetes { get; set; }

        public double BMI { get; set; }

        public double Treatment { get; set; }
    }
}
