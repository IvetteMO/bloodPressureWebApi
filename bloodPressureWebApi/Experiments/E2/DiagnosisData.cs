namespace bloodPressureWebApi.Experiments.E2
{
    public class DiagnosisData
    {
        public int StudyId { get; set; }

        public int Age { get; set; }

        // NUEVO: las 47 lecturas, en orden de fila, para medir la ANN (BPTrend).
        public int[] Systolic { get; set; } = Array.Empty<int>();

        public int[] Diastolic { get; set; } = Array.Empty<int>();

        public int[] Pulse { get; set; } = Array.Empty<int>();

        public double TrendSys { get; set; }

        public double TrendDia { get; set; }

        public double TrendPulse { get; set; }

        public double DayPressureLoad { get; set; }

        public double NightPressureLoad { get; set; }

        public double SystolicNightRatio { get; set; }

        public double DiastolicNightRatio { get; set; }

        // NUEVO: entradas de FHS y CVDRisk (mismos valores por defecto que
        // AIDiagnosisService cuando no hay factores de riesgo registrados).
        public double Sex { get; set; }

        public double Bmi { get; set; } = 25;

        public double Smoking { get; set; }

        public double ParentalHypertension { get; set; }

        public double Diabetes { get; set; }

        public double HypertensionTreatment { get; set; }
    }
}
