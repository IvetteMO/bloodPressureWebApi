namespace bloodPressureWebApi.Experiments.E2
{
    public static class DiagnosisStageNames
    {
        public const string ImportStudy = "ImportStudy";

        public const string ExtractFeatures = "ExtractFeatures";

        // NUEVO: redes neuronales modulares (tendencia SYS/DIA/Pulso).
        public const string BPTrend = "BPTrend";

        public const string HeartRate = "HeartRate";

        public const string BPLevel = "BPLevel";

        public const string BPLoad = "BPLoad";

        public const string NocturnalBP = "NocturnalBP";

        public const string SaveDiagnosis = "SaveDiagnosis";

        public const string Validation = "Validation";

        public const string Nocturnal = "Nocturnal";

        public const string DecisionRules = "DecisionRules";

        // NUEVO: riesgos de Framingham que AIDiagnosisService SI ejecuta en
        // produccion (antes E2 no los medía).
        public const string FHS = "FHS";

        public const string CVDRisk = "CVDRisk";
    }
}