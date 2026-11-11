namespace bloodPressureWebApi.DTOs.Readings
{
    public class ChartReadingDto
    {
        public int ReadingNumber { get; set; }

        public DateTime? ReadingDateTime { get; set; }

        public int Systolic { get; set; }

        public int Diastolic { get; set; }

        public int Pulse { get; set; }

        public string? Period { get; set; }
    }
}
