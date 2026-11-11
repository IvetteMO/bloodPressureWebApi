using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.E1;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Services
{
    /// <summary>
    /// Coordinates E1-B functional equivalence validation
    /// between the ASP.NET backend and the MATLAB reference.
    ///
    /// CAMBIOS RESPECTO A LA VERSION ANTERIOR
    /// 1. Se inyecta E1BBackendTrace y se escribe la traza por etapa
    ///    (antes la clase existia pero NADIE la llamaba; por eso solo
    ///    aparecia el CSV de prueba con la ruta).
    /// 2. Un solo estudio por archivo (el mas reciente). Antes, si el mismo
    ///    Excel se habia importado varias veces (E2..E7), se comparaban
    ///    TODOS los estudios y N quedaba inflado con duplicados.
    /// 3. Se registra la cobertura: que archivos de MATLAB no entraron y por que.
    /// </summary>
    public class E1BExperimentRunner
    {
        // Tolerancias por tipo de variable. Las salidas de ANN y las medias
        // dia/noche en MATLAB estan redondeadas a entero; los FIS y ratios no.
        private const double TolInteger = 1e-9;
        private const double TolContinuous = 1e-6;

        private readonly ApplicationDbContext _context;
        private readonly IE1BReferenceService _referenceService;
        private readonly E1BComparisonService _comparisonService;
        private readonly E1BAnalysisService _analysisService;
        private readonly E1BBackendTrace _trace;

        public E1BExperimentRunner(
            ApplicationDbContext context,
            IE1BReferenceService referenceService,
            E1BComparisonService comparisonService,
            E1BAnalysisService analysisService,
            E1BBackendTrace trace)
        {
            _context = context;
            _referenceService = referenceService;
            _comparisonService = comparisonService;
            _analysisService = analysisService;
            _trace = trace;
        }

        //====================================================
        // RUN E1-B
        //====================================================

        public async Task<E1BExperimentSummaryDto> RunAsync()
        {
            DateTime startedAt = DateTime.Now;
            Guid batchId = Guid.NewGuid();

            _trace.ResetRunFiles();
            _trace.WriteDiagnostic($"E1-B start. BatchId={batchId}. Trace={_trace.TraceDirectory}");

            var references =
                await _referenceService.LoadReferenceAsync();

            if (references.Count == 0)
            {
                throw new InvalidOperationException(
                    "No MATLAB reference records were loaded.");
            }

            var studies = await _context.MAPAStudies
                .Include(x => x.Features)
                .Include(x => x.Diagnosis)
                .AsNoTracking()
                .ToListAsync();

            // Agrupar por nombre de archivo y quedarse con el estudio mas
            // reciente que tenga Features y Diagnosis.
            var studiesByFile = studies
                .Where(s => !string.IsNullOrWhiteSpace(s.ExcelFileName))
                .GroupBy(s => Path.GetFileName(s.ExcelFileName!),
                         StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(),
                              StringComparer.OrdinalIgnoreCase);

            var validationResults = new List<E1BValidationResult>();

            // Recorremos la REFERENCIA (no la BD): asi cada archivo de MATLAB
            // queda contabilizado, entre o no a la comparacion.
            foreach (var (fileName, matlab) in references
                         .OrderBy(r => r.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!studiesByFile.TryGetValue(fileName, out var candidates))
                {
                    _trace.WriteCoverage(batchId, fileName, "NO_BACKEND_STUDY", null, 0,
                        "No existe MAPAStudy con ese ExcelFileName.");
                    continue;
                }

                var study = candidates
                    .Where(s => s.Features != null && s.Diagnosis != null)
                    .OrderByDescending(s => s.StudyId)
                    .FirstOrDefault();

                if (study == null)
                {
                    _trace.WriteCoverage(batchId, fileName, "NO_FEATURES_OR_DIAGNOSIS", null,
                        candidates.Count, "Hay estudio(s), pero ninguno con Features y Diagnosis.");
                    continue;
                }

                try
                {
                    var validation = _comparisonService.Compare(
                        batchId,
                        study.StudyId,
                        study.Features!,
                        study.Diagnosis!,
                        matlab);

                    validationResults.Add(validation);

                    WriteStageTrace(batchId, fileName, study.StudyId, validation);

                    _trace.WriteCoverage(batchId, fileName, "COMPARED", study.StudyId,
                        candidates.Count,
                        candidates.Count > 1
                            ? $"Se uso el estudio mas reciente de {candidates.Count}."
                            : null);
                }
                catch (Exception ex)
                {
                    _trace.WriteError(fileName, study.StudyId, ex);
                    _trace.WriteCoverage(batchId, fileName, "COMPARE_ERROR", study.StudyId,
                        candidates.Count, ex.Message);
                }
            }

            if (validationResults.Count == 0)
            {
                throw new InvalidOperationException(
                    "No studies could be compared. Revise CSHARP_Coverage.csv en " +
                    _trace.TraceDirectory);
            }

            string outputFolder = Path.Combine(
                @"C:\MAPA_E1_TEST",
                "ExperimentResults");

            Directory.CreateDirectory(outputFolder);

            ExportResultsToCsv(validationResults,
                Path.Combine(outputFolder, "E1B_ValidationResults.csv"));

            var statistics = _analysisService.Analyze(
                batchId,
                validationResults);

            ExportStatisticsToCsv(statistics,
                Path.Combine(outputFolder, "E1B_StatisticalSummary.csv"));

            DateTime finishedAt = DateTime.Now;
            int matchedStudies = validationResults.Count;

            int diagnosisMatches =
                validationResults.Count(x => x.BPLevelMatch);

            int nocturnalMatches =
                validationResults.Count(x => x.ProfileMatch);

            _trace.WriteDiagnostic(
                $"E1-B end. References={references.Count}. Compared={matchedStudies}.");

            return new E1BExperimentSummaryDto
            {
                BatchId = batchId,
                Studies = matchedStudies,

                ExactDiagnosisMatches = diagnosisMatches,
                ExactDiagnosisAgreement =
                    100.0 * diagnosisMatches / matchedStudies,

                ExactNocturnalMatches = nocturnalMatches,
                ExactNocturnalAgreement =
                    100.0 * nocturnalMatches / matchedStudies,

                StartedAt = startedAt,
                FinishedAt = finishedAt
            };
        }

        //====================================================
        // TRAZA POR ETAPA (orden del pipeline)
        //====================================================

        private void WriteStageTrace(Guid batchId, string fileName, int studyId,
            E1BValidationResult r)
        {
            // 1) Redes neuronales modulares (salida redondeada)
            _trace.WriteStageComparison(batchId, fileName, studyId, "1_ANN", "SysANN",
                r.MatlabSysANN, r.BackendSysANN, TolInteger);
            _trace.WriteStageComparison(batchId, fileName, studyId, "1_ANN", "DiaANN",
                r.MatlabDiaANN, r.BackendDiaANN, TolInteger);
            _trace.WriteStageComparison(batchId, fileName, studyId, "1_ANN", "PulseANN",
                r.MatlabPulseANN, r.BackendPulseANN, TolInteger);

            // 2) FIS BPLevel (entradas: SysANN, DiaANN)
            _trace.WriteStageComparison(batchId, fileName, studyId, "2_FIS_BPLevel", "BPLevelOutput",
                r.MatlabBPLevel, r.BackendBPLevel, TolContinuous,
                InputsNote(r.MatlabSysANN, r.BackendSysANN, r.MatlabDiaANN, r.BackendDiaANN));

            // 3) FIS HeartRate (entradas: Edad, PulseANN)
            _trace.WriteStageComparison(batchId, fileName, studyId, "3_FIS_HeartRate", "HeartRateOutput",
                r.MatlabHeartRate, r.BackendHeartRate, TolContinuous,
                r.MatlabPulseANN == r.BackendPulseANN
                    ? "PulseANN igual: si difiere, revisar la EDAD que usa el backend."
                    : "PulseANN distinto: la diferencia viene de la ANN.");

            // 4) Separacion dia/noche (MATLAB redondea las medias)
            _trace.WriteStageComparison(batchId, fileName, studyId, "4_DayNight", "Sdia",
                r.MatlabDaySys, r.BackendDaySys, TolInteger, RoundingNote(r.BackendDaySys));
            _trace.WriteStageComparison(batchId, fileName, studyId, "4_DayNight", "Ddia",
                r.MatlabDayDia, r.BackendDayDia, TolInteger, RoundingNote(r.BackendDayDia));
            _trace.WriteStageComparison(batchId, fileName, studyId, "4_DayNight", "Snoche",
                r.MatlabNightSys, r.BackendNightSys, TolInteger, RoundingNote(r.BackendNightSys));
            _trace.WriteStageComparison(batchId, fileName, studyId, "4_DayNight", "Dnoche",
                r.MatlabNightDia, r.BackendNightDia, TolInteger, RoundingNote(r.BackendNightDia));

            // 5) Ratios nocturnos
            _trace.WriteStageComparison(batchId, fileName, studyId, "5_Ratios", "Csis",
                r.MatlabCsis, r.BackendCsis, TolContinuous);
            _trace.WriteStageComparison(batchId, fileName, studyId, "5_Ratios", "Cdia",
                r.MatlabCdia, r.BackendCdia, TolContinuous);

            // 6) FIS Nocturnal + perfil
            _trace.WriteStageComparison(batchId, fileName, studyId, "6_FIS_Nocturnal", "NocturnalOutput",
                r.MatlabNocturnalOutput, r.BackendNocturnalOutput, TolContinuous,
                $"Perfil MATLAB='{r.MatlabProfile}' | Backend='{r.BackendProfile}'");
        }

        private static string InputsNote(double mSys, double bSys, double mDia, double bDia)
            => (mSys == bSys && mDia == bDia)
                ? "Entradas ANN iguales: si difiere, el problema esta en el FIS (archivo .fis / defuzzificacion)."
                : "Entradas ANN distintas: la diferencia se hereda de la etapa 1.";

        private static string RoundingNote(double backend)
            => Math.Abs(backend - Math.Round(backend)) > 1e-9
                ? "Backend NO redondeado; MATLAB usa round(mean(...))."
                : "";

        //====================================================
        // CSV EXPORT (sin cambios)
        //====================================================

        private static void ExportResultsToCsv(
            List<E1BValidationResult> results,
            string outputPath)
        {
            if (results.Count == 0)
                throw new InvalidOperationException(
                    "There are no E1-B results to export.");

            var properties = typeof(E1BValidationResult)
                .GetProperties()
                .Where(p => p.CanRead)
                .ToArray();

            using var writer = new StreamWriter(
                outputPath,
                false,
                new System.Text.UTF8Encoding(true));

            writer.WriteLine(string.Join(",",
                properties.Select(p => EscapeCsv(p.Name))));

            foreach (var result in results)
            {
                var values = properties.Select(p =>
                {
                    object? value = p.GetValue(result);

                    string text = value switch
                    {
                        null => "",
                        DateTime date => date.ToString("O"),
                        double number => number.ToString(
                            "R",
                            System.Globalization.CultureInfo.InvariantCulture),
                        float number => number.ToString(
                            "R",
                            System.Globalization.CultureInfo.InvariantCulture),
                        IFormattable formattable =>
                            formattable.ToString(
                                null,
                                System.Globalization.CultureInfo.InvariantCulture)
                            ?? "",
                        _ => value.ToString() ?? ""
                    };

                    return EscapeCsv(text);
                });

                writer.WriteLine(string.Join(",", values));
            }
        }

        private static string EscapeCsv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static void ExportStatisticsToCsv(
            E1BStatisticsDto statistics,
            string outputPath)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            using var writer = new StreamWriter(
                outputPath,
                false,
                new System.Text.UTF8Encoding(true));

            writer.WriteLine(
                "BatchId,ExperimentCode,ExperimentVersion,Studies," +
                "Metric,Unit,N,MAE,RMSE,MeanBias,MAPE," +
                "StandardDeviation,PearsonR,MedianAbsoluteError," +
                "LowerAgreementLimit,UpperAgreementLimit,MaxAbsoluteError");

            foreach (var metric in statistics.Metrics)
            {
                string[] values =
                {
                    statistics.BatchId.ToString(),
                    statistics.ExperimentCode,
                    statistics.ExperimentVersion,
                    statistics.Studies.ToString(),
                    metric.Metric,
                    metric.Unit,
                    metric.N.ToString(),
                    metric.Mae.ToString("R", inv),
                    metric.Rmse.ToString("R", inv),
                    metric.MeanBias.ToString("R", inv),
                    metric.Mape?.ToString("R", inv) ?? "",
                    metric.StandardDeviation.ToString("R", inv),
                    metric.PearsonR.ToString("R", inv),
                    metric.MedianAbsoluteError?.ToString("R", inv) ?? "",
                    metric.LowerAgreementLimit.ToString("R", inv),
                    metric.UpperAgreementLimit.ToString("R", inv),
                    metric.MaxAbsoluteError.ToString("R", inv)
                };

                writer.WriteLine(string.Join(",",
                    values.Select(EscapeCsv)));
            }
        }
    }
}
