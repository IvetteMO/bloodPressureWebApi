using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Services;

namespace bloodPressureWebApi.Experiments.E2;

public class DiagnosisBenchmark
{
    private readonly MatlabService _matlab;
    private readonly ExperimentProfiler _profiler;

    public DiagnosisBenchmark(
        MatlabService matlab,
        ExperimentProfiler profiler)
    {
        _matlab = matlab;
        _profiler = profiler;
    }

    public DiagnosisBenchmarkResult Execute(DiagnosisData data)
    {
        var result = new DiagnosisBenchmarkResult
        {
            StudyId = data.StudyId
        };

        // =====================================================
        // NUEVO: MODULAR ANN (BPTrend)
        //
        // Antes E2 tomaba la tendencia ya guardada en MAPAFeatures, asi
        // que la red neuronal (componente central del modelo) nunca se
        // medía. Ahora se ejecuta y se usa su salida, igual que en el
        // pipeline de produccion.
        // =====================================================
        double[] trends;

        using (_profiler.Stage(DiagnosisStageNames.BPTrend))
        {
            trends = _matlab.BPTrend(
                data.Systolic,
                data.Diastolic,
                data.Pulse);
        }

        if (trends == null || trends.Length < 3)
        {
            throw new InvalidOperationException(
                $"BPTrend returned fewer than 3 values for study {data.StudyId}.");
        }

        // Verificacion: la ANN debe reproducir la tendencia guardada en BD.
        result.TrendMatchesStored =
            trends[0] == data.TrendSys &&
            trends[1] == data.TrendDia &&
            trends[2] == data.TrendPulse;

        double trendSys = trends[0];
        double trendDia = trends[1];
        double trendPulse = trends[2];

        // =====================================================
        // HEART RATE FUZZY
        // =====================================================
        using (_profiler.Stage(DiagnosisStageNames.HeartRate))
        {
            result.HeartRateRisk =
                _matlab.HeartRate(
                    data.Age,
                    trendPulse);

            result.HeartRateDiagnosis =
                TranslateHeartRateDiagnosis(
                    result.HeartRateRisk);
        }

        // =====================================================
        // BP LEVEL
        // =====================================================
        using (_profiler.Stage(DiagnosisStageNames.BPLevel))
        {
            result.BloodPressureRisk =
                _matlab.BPLevel(
                    trendSys,
                    trendDia);
        }

        // =====================================================
        // PRESSURE LOAD
        // =====================================================
        using (_profiler.Stage(DiagnosisStageNames.BPLoad))
        {
            result.PressureLoadRisk =
                _matlab.BPLoad(
                    data.DayPressureLoad,
                    data.NightPressureLoad);

            result.LoadDiagnosis =
                TranslateLoadDiagnosis(
                    result.PressureLoadRisk);
        }

        // =====================================================
        // NOCTURNAL
        // =====================================================
        using (_profiler.Stage(DiagnosisStageNames.Nocturnal))
        {
            result.NocturnalRisk =
                _matlab.NocturnalBP(
                    data.SystolicNightRatio,
                    data.DiastolicNightRatio);

            if (result.NocturnalRisk >= 0.8 &&
                result.NocturnalRisk <= 0.9)
            {
                result.NocturnalProfile = "Dipper";
            }
            else if (result.NocturnalRisk < 0.8)
            {
                result.NocturnalProfile = "Extreme Dipper";
            }
            else if (result.NocturnalRisk > 1)
            {
                result.NocturnalProfile = "Reverse Dipper";
            }
            else if (result.NocturnalRisk >= 0.901 &&
                     result.NocturnalRisk <= 1)
            {
                result.NocturnalProfile = "Non Dipper";
            }
        }

        // =====================================================
        // EUROPEAN CLASSIFICATION
        // =====================================================
        using (_profiler.Stage(DiagnosisStageNames.DecisionRules))
        {
            double fuzzyBP = result.BloodPressureRisk;

            if (fuzzyBP < 18.25)
                result.DiagnosisLabel = "Hypotension";

            else if (fuzzyBP >= 17 &&
                     fuzzyBP <= 41.6)
                result.DiagnosisLabel = "Optimal";

            else if (fuzzyBP >= 40 &&
                     fuzzyBP <= 49)
                result.DiagnosisLabel = "Normal";

            else if (fuzzyBP >= 48 &&
                     fuzzyBP <= 56)
                result.DiagnosisLabel = "High Normal";

            else if (fuzzyBP >= 54 &&
                     fuzzyBP <= 60)
                result.DiagnosisLabel = "ISH (Grade 1)";

            else if (fuzzyBP >= 58 &&
                     fuzzyBP <= 68)
                result.DiagnosisLabel = "Grade 1 Hypertension";

            else if (fuzzyBP >= 67 &&
                     fuzzyBP <= 72)
                result.DiagnosisLabel = "ISH (Grade 2)";

            else if (fuzzyBP >= 71 &&
                     fuzzyBP <= 79)
                result.DiagnosisLabel = "Grade 2 Hypertension";

            else if (fuzzyBP >= 78 &&
                     fuzzyBP <= 85)
                result.DiagnosisLabel = "ISH (Grade 3)";

            else if (fuzzyBP >= 84 &&
                     fuzzyBP <= 100)
                result.DiagnosisLabel = "Grade 3 Hypertension";
        }

        // Framingham (FHS / CVDRisk) NO se ejecuta: los factores de riesgo
        // de los pacientes no estan disponibles (se usarian valores por
        // defecto) y esas funciones quedan fuera del modelo validado en E1-B.

        return result;
    }

    private string TranslateHeartRateDiagnosis(double value)
    {
        if (value >= 0 && value <= 14.96)
            return "Hypotension";

        if (value > 14.96 && value <= 28)
            return "Below Normal";

        if (value >= 28.01 && value <= 43.73)
            return "Excellent";

        if (value >= 43.74 && value <= 72.69)
            return "Above Normal";

        return "Very High";
    }

    private string TranslateLoadDiagnosis(double value)
    {
        if (value <= 20)
        {
            return "Normal";
        }

        if (value >= 20.1 &&
            value <= 40)
        {
            return "Intermediate";
        }

        return "High Load";
    }
}