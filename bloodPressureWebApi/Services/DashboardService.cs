using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Dashboard;

using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Services
{
    public class DashboardService
    {
        private readonly
            ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================
        // OVERVIEW
        // =====================================

        public async Task<DashboardOverviewDto>GetOverview()
        {
            var totalStudies =
                await _context
                .AIDiagnosis
                .CountAsync();

            var highRisk =
                await _context
                .AIDiagnosis
                .CountAsync(
                    x =>
                        x.CardiovascularRisk >= 20);

            var nonDippers =
                await _context
                .AIDiagnosis
                .CountAsync(
                    x =>
                        x.NocturnalProfile ==
                        "Non Dipper");

            var today =
                DateTime.Today;

            var criticalToday =
                await _context
                .AIDiagnosis
                .CountAsync(
                    x =>
                        x.CreatedAt.Date ==
                        today
                        &&
                        x.CardiovascularRisk >= 20);

            return new
                DashboardOverviewDto
            {
                HighRiskPatients =
                    highRisk,

                NonDipperPercent =
                    totalStudies == 0
                    ? 0
                    : Math.Round(
                        ((double)
                        nonDippers /
                        totalStudies)
                        * 100,
                        2),

                CriticalStudiesToday =
                    criticalToday,

                TotalAIInferences =
                    totalStudies
            };
        }

        // =====================================
        // ALERTS
        // =====================================

        public async Task< List<DashboardAlertDto>>
            GetAlerts()
        {
            var nightHTA =
                await _context
                .MAPAFeatures
                .CountAsync(
                    x =>
                        x.NightSystolicMean >= 120);

            var reverseDippers =
                await _context
                .AIDiagnosis
                .CountAsync(
                    x =>
                        x.NocturnalProfile ==
                        "Reverse Dipper");

            var highPulse =
                await _context
                .MAPAFeatures
                .CountAsync(
                    x =>
                        x.PulseTrend >= 90);

            var escalation =
                await _context
                .AIDiagnosis
                .CountAsync(
                    x =>
                        x.CardiovascularRisk >= 30);

            return new
                List<DashboardAlertDto>
            {
                new()
                {
                    Title =
                        "Night Hypertension",

                    Count =
                        nightHTA
                },

                new()
                {
                    Title =
                        "Reverse Dipper Profiles",

                    Count =
                        reverseDippers
                },

                new()
                {
                    Title =
                        "High Pulse Variability",

                    Count =
                        highPulse
                },

                new()
                {
                    Title =
                        "Cardiovascular Escalation",

                    Count =
                        escalation
                }
            };
        }

        // =====================================
        // INSIGHTS
        // =====================================

        public async Task<List<string>>GetInsights(int studyId)
        {
            var diagnosis =
                await _context
                .AIDiagnosis
                .FirstOrDefaultAsync(
                    x =>
                        x.StudyId ==
                        studyId);

            if (diagnosis == null
                ||
                string.IsNullOrWhiteSpace(
                    diagnosis.Explanations))
            {
                return new
                    List<string>();
            }

            return diagnosis
                .Explanations
                .Split('|')
                .Select(x =>
                    x.Trim())
                .ToList();
        }
    }
}