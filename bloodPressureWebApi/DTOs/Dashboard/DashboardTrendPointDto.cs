// ============================================
// DashboardTrendPointDto.cs
// ============================================

namespace bloodPressureWebApi.DTOs.Dashboard
{
    public class DashboardTrendPointDto
    {
        public string Time { get; set; }

        public double Systolic { get; set; }

        public double Diastolic { get; set; }

        public double Pulse { get; set; }
    }
}