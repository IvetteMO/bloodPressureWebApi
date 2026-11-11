using bloodPressureWebApi.DTOs.Shared;

namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class FullDiagnosisRequestDto
    {
        public int Age { get; set; }

        public int Sex { get; set; }

        public double BMI { get; set; }

        public int Smoker { get; set; }

        public int Parents { get; set; }

        public int Diabetes { get; set; }

        public int Treatment { get; set; }

        public int DayStartHour { get; set; }

        public int NightStartHour { get; set; }

        public List<BloodPressureReadingDto>
            Readings
        { get; set; }
            = new();
    }
}