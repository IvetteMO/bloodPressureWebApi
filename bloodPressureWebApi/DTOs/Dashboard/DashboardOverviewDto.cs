namespace bloodPressureWebApi.DTOs.Dashboard
{
    public class DashboardOverviewDto
    {
            public int HighRiskPatients { get; set; }

            public double NonDipperPercent { get; set; }

            public int CriticalStudiesToday { get; set; }

            public int TotalAIInferences { get; set; }
    }
}
