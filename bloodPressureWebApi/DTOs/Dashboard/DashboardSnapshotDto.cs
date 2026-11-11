namespace bloodPressureWebApi.DTOs.Dashboard
{
    public class DashboardSnapshotDto
    {
        public int HighRiskPatients { get; set; }

        public double NonDipperRate { get; set; }

        public int CriticalStudiesToday { get; set; }
    }
}
