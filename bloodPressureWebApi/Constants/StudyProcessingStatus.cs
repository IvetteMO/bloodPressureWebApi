namespace bloodPressureWebApi.Constants
{
    public static class StudyProcessingStatus
    {
        public const string Uploaded =
            "UPLOADED";

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
    }
}