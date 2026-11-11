namespace bloodPressureWebApi.DTOs.Dashboard
{
    public class BPTrendPointDto
    {
        public string Time { get; set; } = "";

        public double Systolic { get; set; }

        public double Diastolic { get; set; }
    }
}
