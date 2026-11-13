using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Admin;
using bloodPressureWebApi.Infrastructure.Matlab;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    /// <summary>
    /// Panel de administración: auditoría y estado del sistema.
    /// Solo rol ADMIN. (Los usuarios están en UsersController.)
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public sealed class AdminController(ApplicationDbContext context) : ControllerBase
    {
        private const int MaxPageSize = 100;

        // =====================================================
        // AUDIT
        // GET api/admin/audit?userId=3&from=2026-11-01&to=2026-11-12&page=1&pageSize=25
        // =====================================================

        [HttpGet("audit")]
        [ProducesResponseType<PagedResponse<AuditEntryResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAudit(
            [FromQuery] int? userId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = context.AuditLogs.AsNoTracking();

            if (userId is not null)
            {
                query = query.Where(x => x.UserId == userId);
            }

            if (from is not null)
            {
                query = query.Where(x => x.CreatedAt >= from.Value.Date);
            }

            if (to is not null)
            {
                // "hasta" incluye todo ese día
                var end = to.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < end);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.Action.Contains(term)
                    || (x.UserEmail != null && x.UserEmail.Contains(term))
                    || (x.Path != null && x.Path.Contains(term)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.AuditLogId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditEntryResponse(
                    x.AuditLogId,
                    x.CreatedAt,
                    x.UserId,
                    x.UserEmail,
                    x.UserRole,
                    x.Action,
                    x.EntityName,
                    x.EntityId,
                    x.HttpMethod,
                    x.Path,
                    x.StatusCode,
                    x.IpAddress))
                .ToListAsync(cancellationToken);

            return Ok(new PagedResponse<AuditEntryResponse>(items, page, pageSize, totalCount));
        }

        // =====================================================
        // SYSTEM STATUS
        // =====================================================

        [HttpGet("system-status")]
        [ProducesResponseType<SystemStatusResponse>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSystemStatus(
            [FromServices] MatlabReadiness matlab,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var today = DateTime.Now.Date;
            var weekAgo = today.AddDays(-6);
            var inProgress = StudyProcessingStatus.InProgress.ToList();

            // Consultas secuenciales: un DbContext no admite consultas en paralelo.
            var studiesInProgress = await context.MAPAStudies
                .CountAsync(x => x.ProcessingStatus != null && inProgress.Contains(x.ProcessingStatus), cancellationToken);

            var studiesFailed = await context.MAPAStudies
                .CountAsync(x => x.ProcessingStatus == StudyProcessingStatus.Failed, cancellationToken);

            var totalPatients = await context.Patients.CountAsync(cancellationToken);

            var totalStudies = await context.MAPAStudies.CountAsync(cancellationToken);

            var diagnosesToday = await context.AIDiagnosis
                .CountAsync(x => x.CreatedAt >= today, cancellationToken);

            var diagnosesWeek = await context.AIDiagnosis
                .CountAsync(x => x.CreatedAt >= weekAgo, cancellationToken);

            var activeUsers = await context.AppUsers
                .CountAsync(x => x.IsActive, cancellationToken);

            var loginsWeek = await context.AuditLogs
                .CountAsync(x => x.Action == "LOGIN" && x.CreatedAt >= weekAgo, cancellationToken);

            var failedLoginsWeek = await context.AuditLogs
                .CountAsync(x => x.Action == "LOGIN_FAILED" && x.CreatedAt >= weekAgo, cancellationToken);

            var recentFailures = await context.MAPAStudies
                .AsNoTracking()
                .Where(x => x.ProcessingStatus == StudyProcessingStatus.Failed)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => new FailedStudyResponse(
                    x.StudyId,
                    x.PatientId,
                    x.ProcessingError,
                    x.CreatedAt))
                .ToListAsync(cancellationToken);

            var warmupEnabled = configuration.GetValue("Matlab:WarmupOnStartup", true);

            return Ok(new SystemStatusResponse(
                MatlabReady: matlab.IsReady,
                MatlabWarmupEnabled: warmupEnabled,
                MatlabInitializationMs: matlab.InitializationMs,
                MatlabError: matlab.Error,
                StudiesInProgress: studiesInProgress,
                StudiesFailed: studiesFailed,
                TotalPatients: totalPatients,
                TotalStudies: totalStudies,
                DiagnosesToday: diagnosesToday,
                DiagnosesLast7Days: diagnosesWeek,
                ActiveUsers: activeUsers,
                LoginsLast7Days: loginsWeek,
                FailedLoginsLast7Days: failedLoginsWeek,
                RecentFailures: recentFailures));
        }
    }
}
