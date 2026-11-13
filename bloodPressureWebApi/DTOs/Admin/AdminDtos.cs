namespace bloodPressureWebApi.DTOs.Admin
{
    public sealed record AuditEntryResponse(
        int AuditId,
        DateTime CreatedAt,
        int? UserId,
        string? UserEmail,
        string? UserRole,
        string Action,
        string EntityName,
        int EntityId,
        string? HttpMethod,
        string? Path,
        int? StatusCode,
        string? IpAddress);

    public sealed record PagedResponse<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount);

    public sealed record FailedStudyResponse(
        int StudyId,
        int PatientId,
        string? ProcessingError,
        DateTime CreatedAt);

    /// <summary>Estado de la app para el panel de administración.</summary>
    public sealed record SystemStatusResponse(
        bool MatlabReady,
        bool MatlabWarmupEnabled,
        double? MatlabInitializationMs,
        string? MatlabError,
        int StudiesInProgress,
        int StudiesFailed,
        int TotalPatients,
        int TotalStudies,
        int DiagnosesToday,
        int DiagnosesLast7Days,
        int ActiveUsers,
        int LoginsLast7Days,
        int FailedLoginsLast7Days,
        IReadOnlyList<FailedStudyResponse> RecentFailures);
}
