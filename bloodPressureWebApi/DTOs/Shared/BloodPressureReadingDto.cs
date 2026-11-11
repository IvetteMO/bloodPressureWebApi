namespace bloodPressureWebApi.DTOs.Shared
{
    public class BloodPressureReadingDto
    {
        public DateTime ReadingTime { get; set; }

        public int Systolic { get; set; }

        public int Diastolic { get; set; }

        public int Pulse { get; set; }
    }
}
