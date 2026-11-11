namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class HyperRiskRequestDto
    {
        public int Age { get; set; }

        public int Sex { get; set; }

        public double BMI { get; set; }

        public int Smoker { get; set; }

        public int Diabetes { get; set; }

        public int Parents { get; set; }

        public List<int> SystolicReadings
        { get; set; } = new();

        public List<int> DiastolicReadings
        { get; set; } = new();
    }
}