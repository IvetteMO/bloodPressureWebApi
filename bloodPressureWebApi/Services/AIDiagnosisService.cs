using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using Microsoft.EntityFrameworkCore;
using bloodPressureWebApi.Experiments.E5.FaultInjection;

namespace bloodPressureWebApi.Services
{
    public class AIDiagnosisService
    {
        private readonly ApplicationDbContext _context;
        private readonly MatlabService _matlab;
        private readonly IFaultInjectionService _faultInjection;

        public AIDiagnosisService(ApplicationDbContext context, MatlabService matlab, IFaultInjectionService faultInjection)
        {
            _context = context;

            _matlab = matlab;

            _faultInjection = faultInjection;
        }

        // =====================================================
        // RUN DIAGNOSIS
        // =====================================================
        // Firma original: la usan el front, los controladores y Hangfire
        // (BackgroundJob.Enqueue). Comportamiento de produccion SIN CAMBIOS.
        public Task RunDiagnosis(int studyId)
            => RunDiagnosis(studyId, includeFramingham: true);

        // Sobrecarga para experimentacion: los experimentos pasan false porque
        // los factores de riesgo no estan disponibles y Framingham queda fuera
        // del modelo evaluado en el articulo. (Sobrecarga y no parametro
        // opcional: Hangfire usa arboles de expresion, que no admiten
        // argumentos opcionales.)
        public async Task RunDiagnosis(int studyId, bool includeFramingham)
        {
            var feature = await _context.MAPAFeatures
                .FirstOrDefaultAsync(x => x.StudyId == studyId);

            if (feature == null)
            {
                throw new Exception("Features not found");
            }

            var study = await _context.MAPAStudies
                .Include(x => x.Patient)
                .Include(x => x.RiskFactors)
                .Include(x => x.Readings)
                .FirstOrDefaultAsync(x =>
                    x.StudyId == studyId);

            if (study == null)
            {
                throw new Exception("Study not found");
            }

            var risk = study.RiskFactors
                .OrderByDescending(x => x.CreatedAt)
                   .FirstOrDefault();

            int age = study.Age
                ??
                (
                  study.Patient?.BirthDate != null
                  ?
                  DateTime.Now.Year
                  -
                  study.Patient.BirthDate.Value.Year
                  :
                  40
                );


            // =====================================================
            // BP TREND ANN
            // =====================================================

            double trendSys = feature.SystolicTrend ?? 0;

            double trendDia = feature.DiastolicTrend ?? 0;

            double trendPulse = feature.PulseTrend ?? 0;

            // =====================================================
            // HEART RATE FUZZY
            // =====================================================

            _faultInjection.ThrowIfRequired();

            double hrRisk = _matlab.HeartRate(age, trendPulse);

            string heartRateDiagnosis = TranslateHeartRateDiagnosis(hrRisk);

            // =====================================================
            // FUZZY BP
            // =====================================================

            double fuzzyBP = _matlab.BPLevel(trendSys, trendDia);

            // =====================================================
            // FUZZY LOAD
            // =====================================================

            double fuzzyLoad = _matlab.BPLoad(
                    feature.DayPressureLoad ?? 0,
                    feature.NightPressureLoad ?? 0);

            string loadDiagnosis = TranslateLoadDiagnosis(fuzzyLoad);

            // =====================================================
            // FUZZY NOCTURNAL
            // =====================================================

            double fuzzyNocturnal = _matlab.NocturnalBP(
                    feature.SystolicNightRatio ?? 0,
                    feature.DiastolicNightRatio ?? 0);

            // =====================================================
            // NOCTURNAL PROFILE
            // =====================================================

            string nocturnalProfile = "Unknown";

            if (fuzzyNocturnal >= 0.8
                &&
                fuzzyNocturnal <= 0.9)
            {
                nocturnalProfile = "Dipper";
            }
            else if (fuzzyNocturnal < 0.8)
            {
                nocturnalProfile = "Extreme Dipper";
            }
            else if (fuzzyNocturnal > 1)
            {
                nocturnalProfile = "Reverse Dipper";
            }
            else if (fuzzyNocturnal >= 0.901
                &&
                fuzzyNocturnal <= 1)
            {
                nocturnalProfile = "Non Dipper";

            }

            // =====================================================
            // HYPERTENSION 4 YEAR RISK
            // =====================================================

            double hypertensionRisk = 0;
            double[] cvd = Array.Empty<double>();

            if (includeFramingham)
            {
                hypertensionRisk = _matlab.FHS(age,
                        Convert.ToDouble(
                            study.Patient?.Sex ?? 0),

                        risk?.BMI ?? 25,

                        trendSys,
                        trendDia,

                        Convert.ToDouble(
                            risk?.Smoking ?? 0
                        ),

                        Convert.ToDouble(
                            risk?.ParentalHypertension ?? 0
                        )
                    );

                // =====================================================
                // CVD 10 YEAR RISK
                // =====================================================

                cvd = _matlab.CVDRisk(
                        Convert.ToDouble(
                            study.Patient?.Sex ?? 0),

                        age,

                        trendSys,

                        Convert.ToDouble(
                            risk?.Smoking ?? 0),

                        Convert.ToDouble(
                            risk?.Diabetes ?? 0),

                        risk?.BMI ?? 25,

                        Convert.ToDouble(
                            risk?.HypertensionTreatment ?? 0)
                    );
            }
            // =====================================================
            // EUROPEAN FUZZY CLASSIFICATION
            // =====================================================
            string diagnosisLabel = "Unknown";

            if (fuzzyBP < 18.25)
            {
                diagnosisLabel = "Hypotension";
            }
            else if (fuzzyBP >= 17
                &&
                fuzzyBP <= 41.6)
            {
                diagnosisLabel = "Optimal";
            }
            else if (fuzzyBP >= 40
                &&
                fuzzyBP <= 49)
            {
                diagnosisLabel = "Normal";
            }
            else if (fuzzyBP >= 48
                &&
                fuzzyBP <= 56)
            {
                diagnosisLabel = "High Normal";
            }
            else if (fuzzyBP >= 54
                &&
                fuzzyBP <= 60)
            {
                diagnosisLabel = "ISH (Grade 1)";
            }
            else if (fuzzyBP >= 58
                &&
                fuzzyBP <= 68)
            {
                diagnosisLabel = "Grade 1 Hypertension";
            }
            else if (fuzzyBP >= 67
                &&
                fuzzyBP <= 72)
            {
                diagnosisLabel = "ISH (Grade 2)";
            }
            else if (fuzzyBP >= 71
                &&
                fuzzyBP <= 79)
            {
                diagnosisLabel = "Grade 2 Hypertension";
            }
            else if (fuzzyBP >= 78
                &&
                fuzzyBP <= 85)
            {
                diagnosisLabel = "ISH (Grade 3)";
            }
            else if (fuzzyBP >= 84
                &&
                fuzzyBP <= 100)
            {
                diagnosisLabel = "Grade 3 Hypertension";
            }

            // =====================================================
            // UPDATE OR CREATE
            // =====================================================

            var diagnosis = await _context.AIDiagnosis
                .FirstOrDefaultAsync(x => x.StudyId == studyId);

            if (diagnosis == null)
            {
                diagnosis = new AIDiagnosis
                {
                    StudyId =
                            studyId
                };

                _context.AIDiagnosis.Add(diagnosis);
            }

            diagnosis.FuzzyBPRisk = fuzzyBP;

            diagnosis.FuzzyLoadC = fuzzyLoad;

            diagnosis.FuzzyNocturnalC = fuzzyNocturnal;

            diagnosis.NocturnalProfile = nocturnalProfile;

            diagnosis.FuzzyHeartRC = hrRisk;

            diagnosis.LoadDiagnosis = loadDiagnosis;

            diagnosis.HeartRateDiagnosis = heartRateDiagnosis;

            if (includeFramingham)
            {
                diagnosis.Hypertension4YearRisk = hypertensionRisk;

                double cardiovascularRisk = cvd.Length > 0
                    ? Math.Round(cvd[0])
                    : 0;

                double heartAge = cvd.Length > 1
                  ? Math.Round(cvd[1])
                  : 0;

                diagnosis.CardiovascularRisk = cardiovascularRisk;

                diagnosis.HeartAge = heartAge;
            }

            diagnosis.FinalRiskLevel = diagnosisLabel;

            diagnosis.ModelVersion = ModelVersions.GlobalModel;

            diagnosis.FeaturePipelineVersion = ModelVersions.FeaturePipeline;

            // CORRECCION: antes era la constante "MATLAB Runtime 2026".
            // Una constante escrita en el codigo siempre "coincide", por lo
            // que E7 no puede reportarla como evidencia de consistencia del
            // runtime. Lea la version real (configuracion del despliegue o
            // la que exponga MatlabService) y guardela aqui.
            diagnosis.MatlabRuntimeVersion =
                Environment.GetEnvironmentVariable("MATLAB_RUNTIME_VERSION")
                ?? "UNKNOWN";

            // CORRECCION: antes ConfidenceScore = 0.95 fijo para TODOS los
            // diagnosticos. No es una confianza calculada por el modelo; no
            // debe guardarse ni reportarse. Se deja sin asignar.
            // diagnosis.ConfidenceScore = 0.95;

            diagnosis.CreatedAt = DateTime.Now;

            study.StudyStatus = "ANALYZED";

            //_context.MAPAStudies
            //        .Where(x => x.StudyId == studyId)
            //        .ExecuteUpdate(s => s
            //            .SetProperty(
            //                x => x.StudyStatus,
            //                "ANALYZED"
            //            ));

            var explanations = GenerateExplanations(
                  trendPulse,
                  fuzzyLoad,
                  hrRisk,
                  nocturnalProfile,
                  risk);

            diagnosis.Explanations = string.Join(" | ",
                    explanations);


            await _context.SaveChangesAsync();
        }

        private List<string> GenerateExplanations(double trendPulse,
            double fuzzyLoad,
            double hrRisk,
            string nocturnalProfile,
            PatientRiskFactor? risk)
        {
            List<string> explanations = new List<string>();

            // =====================================
            // NOCTURNAL
            // =====================================

            if (nocturnalProfile == "Non Dipper")
            {
                explanations.Add("Non-dipper nocturnal profile detected");
            }

            // =====================================
            // PULSE
            // =====================================

            if (trendPulse > 80)
            {
                explanations.Add("Elevated pulse trend detected");
            }

            // =====================================
            // LOAD
            // =====================================

            if (fuzzyLoad > 20)
            {
                explanations.Add("Elevated blood pressure load detected");
            }

            // =====================================
            // BMI
            // =====================================

            if ((risk?.BMI ?? 0) >= 30)
            {
                explanations.Add("Obesity contributes to cardiovascular risk");
            }

            // =====================================
            // SMOKING
            // =====================================

            if ((risk?.Smoking ?? 0) == 1)
            {
                explanations.Add("Smoking contributes to cardiovascular risk");
            }

            // =====================================
            // DIABETES
            // =====================================

            if ((risk?.Diabetes ?? 0) == 1)
            {
                explanations.Add("Diabetes risk factor detected");
            }

            // =====================================
            // FAMILY HISTORY
            // =====================================

            if ((risk?.ParentalHypertension ?? 0) > 0)
            {
                explanations.Add("Family hypertension history detected");
            }

            // =====================================
            // HEART RATE
            // =====================================

            if (hrRisk > 43)
            {
                explanations.Add("Elevated heart rate cardiovascular pattern detected");
            }

            return explanations;
        }

        // =====================================================
        // HEART RATE DIAGNOSIS
        // =====================================================

        private string TranslateHeartRateDiagnosis(
            double value)
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


        // =====================================================
        // PRESSURE LOAD DIAGNOSIS
        // =====================================================

        private string TranslateLoadDiagnosis(double value)
        {
            if (value <= 20)
            {
                return "Normal";
            }

            if (value >= 20.1
                &&
                value <= 40)
            {
                return "Intermediate";

            }

            return "High Load";
        }
    }
}