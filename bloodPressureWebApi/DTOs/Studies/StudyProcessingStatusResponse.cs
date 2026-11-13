namespace bloodPressureWebApi.DTOs.Studies
{
    /// <summary>
    /// Estado del diagnóstico de un estudio. Lo consulta el frontend cada
    /// pocos segundos mientras IsProcessing = true (respuesta mínima, sin
    /// lecturas ni diagnóstico).
    ///
    /// ProcessingError solo trae el detalle técnico en Development; en
    /// producción es un mensaje genérico (el detalle queda en la BD/log).
    /// </summary>
    public sealed record StudyProcessingStatusResponse(
        int StudyId,
        string? ProcessingStatus,
        bool IsProcessing,
        bool HasFailed,
        string? ProcessingError);
}
