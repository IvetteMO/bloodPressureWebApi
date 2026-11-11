using System.Diagnostics;

namespace bloodPressureWebApi.Services
{
    public class PerformanceEvaluationService
    {
        private readonly AIDiagnosisService _diagnosis;

        public PerformanceEvaluationService(AIDiagnosisService diagnosis)
        {
            _diagnosis = diagnosis;
        }

        public async Task<double>  EvaluateInferenceTime(int studyId)
        {
            Stopwatch sw = Stopwatch.StartNew();

            await _diagnosis.RunDiagnosis(studyId);

            sw.Stop();

            return sw.ElapsedMilliseconds;
        }
    }
}
