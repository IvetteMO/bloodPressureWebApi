namespace bloodPressureWebApi.Constants
{
    public static class ModelVersions
    {
        // ======================================
        // PIPELINE
        // ======================================

        public const string
            FeaturePipeline =
            "FeaturePipeline_v2.1";

        // ======================================
        // NEURAL NETWORKS
        // ======================================

        public const string
            FHS =
            "FHS_ANN_redesFPA1908_v76";

        public const string
            CVDGeneral =
            "CVD_ANN_redesRicvdFPA2309_v22";

        public const string
            CVDTreatment =
            "CVD_ANN_redesECcvdCRFPA0611_v15";

        public const string
            CVDNoTreatment =
            "CVD_ANN_redesECcvdSRFPA0711_v21";

        public const string
            BPTrend =
            "BPTrend_train26june01";

        // ======================================
        // FUZZY
        // ======================================

        public const string
            FuzzyBP =
            "Hipertrap_v1";

        public const string
            FuzzyNocturnal =
            "BestFBSA0507-10";

        public const string
            FuzzyHeartRate =
            "BestBSA1102-21";

        public const string
            FuzzyLoad =
            "BPLoad_v1";

        // ======================================
        // GLOBAL
        // ======================================

        public const string
            GlobalModel =
            "SoftComputingHybrid_v3.0";
    }
}