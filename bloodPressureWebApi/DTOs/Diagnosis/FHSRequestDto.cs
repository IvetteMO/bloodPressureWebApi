namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class FHSRequestDto
    {
        public double Age { get; set; }

        public double Sex { get; set; }

        public double BMI { get; set; }

        public double Systolic { get; set; }

        public double Diastolic { get; set; }

        public double Smoker { get; set; }

        public double Parents { get; set; }
    }
}
