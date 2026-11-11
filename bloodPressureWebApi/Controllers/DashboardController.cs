using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Dashboard;
using bloodPressureWebApi.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboard;
        private readonly ApplicationDbContext _context;

        public DashboardController(DashboardService dashboard, ApplicationDbContext context)
        {
            _dashboard =
                dashboard;
            _context = context;
        }

        // =====================================
        // OVERVIEW
        // =====================================

        [HttpGet("overview")]
        public async Task<IActionResult>Overview()
        {
            return Ok(
                await _dashboard
                .GetOverview());
        }

        // =====================================
        // ALERTS
        // =====================================

        [HttpGet("alerts")]
        public async Task<IActionResult>Alerts()
        {
            return Ok(
                await _dashboard
                .GetAlerts());
        }

        // =====================================
        // INSIGHTS
        // =====================================

        [HttpGet("insights/{studyId}")] public async Task<IActionResult> Insights( int studyId)
        {
            return Ok(
                await _dashboard
                .GetInsights(
                    studyId));
        }

        // ============================================
        // TRENDS
        // ============================================

        [HttpGet("trends/{studyId}")]
        public async Task<ActionResult<IEnumerable<DashboardTrendPointDto>>> GetTrendData(int studyId)
        {
            var readings =
                await _context.MAPAReadings

                .Where(r =>
                    r.StudyId == studyId)

                .OrderBy(r =>
                    r.ReadingDateTime)

                .Take(48)

                .Select(r =>
                    new DashboardTrendPointDto
                    {
                        Time =
                            r.ReadingDateTime
                            .ToString("HH:mm"),

                        Systolic =
                            r.Systolic,

                        Diastolic =
                            r.Diastolic,

                        Pulse =
                            r.Pulse
                    })

                .ToListAsync();

            return Ok(readings);
        }

        // ============================================
        // SNAPSHOT
        // ============================================

        [HttpGet("snapshot")]
        public async Task<ActionResult<DashboardSnapshotDto>>GetSnapshot()
        {
            var totalStudies =
                await _context
                    .AIDiagnosis
                    .CountAsync();

            var highRiskPatients =
                await _context
                    .AIDiagnosis

                    .CountAsync(d =>
                        d.FinalRiskLevel ==
                        "High");

            var nonDipperCount =
                await _context
                    .AIDiagnosis

                    .CountAsync(d =>
                        d.NocturnalProfile ==
                        "Non Dipper");

            var criticalToday =
                await _context
                    .AIDiagnosis

                    .CountAsync(d =>

                        d.CreatedAt.Date ==
                        DateTime.UtcNow.Date
                    );

            var dto =
                new DashboardSnapshotDto
                {
                    HighRiskPatients =
                        highRiskPatients,

                    NonDipperRate =

                        totalStudies == 0
                            ? 0
                            : Math.Round(
                                (double)
                                nonDipperCount
                                / totalStudies
                                * 100,
                                1
                            ),

                    CriticalStudiesToday =
                        criticalToday
                };

            return Ok(dto);
        }
    }
}