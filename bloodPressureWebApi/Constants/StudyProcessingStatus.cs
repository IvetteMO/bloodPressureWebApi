namespace bloodPressureWebApi.Constants
{
    public static class StudyProcessingStatus
    {
        public const string Uploaded =
            "UPLOADED";

        // Recalculo pendiente en la cola (p. ej. tras editar factores de riesgo)
        public const string Queued =
            "QUEUED";

        public const string Processing =
            "PROCESSING";

        public const string FeatureExtraction =
            "FEATURE_EXTRACTION";

        public const string AIAnalysis =
            "AI_ANALYSIS";

        public const string Completed =
            "COMPLETED";

        public const string Failed =
            "FAILED";

        // Estados en los que el worker todavia esta trabajando (o el job
        // esta en la cola). UPLOADED no cuenta: los estudios importados
        // con el flujo anterior se quedaban asi aunque ya tuvieran
        // diagnostico.
        public static readonly IReadOnlySet<string> InProgress =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Queued,
                Processing,
                FeatureExtraction,
                AIAnalysis
            };

        public static bool IsInProgress(string? status) =>
            status is not null && InProgress.Contains(status);
    }
}