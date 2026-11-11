namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class NocturnalRequestDto
    {
        public double DaySystolic { get; set; }

        public double NightSystolic { get; set; }

        public double DayDiastolic { get; set; }

        public double NightDiastolic { get; set; }
    }
}
