using bloodPressureWebApi.Models;

namespace bloodPressureWebApi.Experiments.E2
{
    /// <summary>
    /// Construye el DiagnosisData de un estudio de forma IDENTICA para E2, E3,
    /// E4... (antes cada runner tenia su propia copia y E3 se quedaba con la
    /// version vieja: sin lecturas, edad 0 por defecto).
    /// Requiere que el estudio venga con .Include(Features), .Include(Readings),
    /// .Include(Patient) e .Include(RiskFactors).
    /// </summary>
    public static class DiagnosisDataLoader
    {
        public const int RequiredReadings = 47;

        public static DiagnosisData Load(MAPAStudy study)
        {
            if (study.Features == null)
            {
                throw new InvalidOperationException(
                    $"Study {study.StudyId} has no extracted features.");
            }

            var readings = (study.Readings ?? new List<MAPAReading>())
                .OrderBy(x => x.ReadingId)
                .ToList();

            if (readings.Count != RequiredReadings)
            {
                throw new InvalidOperationException(
                    $"Study {study.StudyId} has {readings.Count} readings; {RequiredReadings} expected.");
            }

            // Mismos valores por defecto que AIDiagnosisService
            var risk = (study.RiskFactors ?? new List<PatientRiskFactor>())
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            return new DiagnosisData
            {
                Sex = Convert.ToDouble(study.Patient?.Sex ?? 0),
                Bmi = risk?.BMI ?? 25,
                Smoking = Convert.ToDouble(risk?.Smoking ?? 0),
                ParentalHypertension = Convert.ToDouble(risk?.ParentalHypertension ?? 0),
                Diabetes = Convert.ToDouble(risk?.Diabetes ?? 0),
                HypertensionTreatment = Convert.ToDouble(risk?.HypertensionTreatment ?? 0),

                StudyId = study.StudyId,
                Age = study.Age ?? 40,

                Systolic = readings.Select(x => x.Systolic).ToArray(),
                Diastolic = readings.Select(x => x.Diastolic).ToArray(),
                Pulse = readings.Select(x => x.Pulse).ToArray(),

                TrendSys = study.Features.SystolicTrend ?? 0,
                TrendDia = study.Features.DiastolicTrend ?? 0,
                TrendPulse = study.Features.PulseTrend ?? 0,

                DayPressureLoad = study.Features.DayPressureLoad ?? 0,
                NightPressureLoad = study.Features.NightPressureLoad ?? 0,

                SystolicNightRatio = study.Features.SystolicNightRatio ?? 0,
                DiastolicNightRatio = study.Features.DiastolicNightRatio ?? 0
            };
        }
    }
}
