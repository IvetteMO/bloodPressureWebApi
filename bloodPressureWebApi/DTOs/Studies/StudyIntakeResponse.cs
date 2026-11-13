namespace bloodPressureWebApi.DTOs.Studies
{
    /// <summary>
    /// Resultado del alta: el estudio ya quedó importado y validado.
    /// El diagnóstico se calcula en la cola (ProcessingStatus = QUEUED);
    /// el frontend lo sigue con GET api/Studies/{id}/status.
    /// </summary>
    public sealed record StudyIntakeResponse(
        int PatientId,
        int StudyId,
        string? StudyStatus,
        string? ProcessingStatus);
}
