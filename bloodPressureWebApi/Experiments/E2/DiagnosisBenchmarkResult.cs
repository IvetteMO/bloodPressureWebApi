namespace bloodPressureWebApi.Experiments.E2
{
    public class DiagnosisBenchmarkResult
    {
        public int StudyId { get; set; }
        public double HeartRateRisk { get; set; }

        public double BloodPressureRisk { get; set; }

        public double PressureLoadRisk { get; set; }

        public double NocturnalRisk { get; set; }

        public double HypertensionRisk { get; set; }

        public double CardiovascularRisk { get; set; }

        public double HeartAge { get; set; }

        public string DiagnosisLabel { get; set; } = string.Empty;

        public string NocturnalProfile { get; set; } = string.Empty;

        public string LoadDiagnosis { get; set; } = string.Empty;

        public string HeartRateDiagnosis { get; set; } = string.Empty;

        public int Iteration { get; set; }

        // NUEVO: la ANN recalculada coincide con la tendencia guardada en BD.
        public bool TrendMatchesStored { get; set; }
    }
}
