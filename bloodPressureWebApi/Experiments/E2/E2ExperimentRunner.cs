using bloodPressureWebApi.Data;
using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Experiments.E2
{
    public class E2ExperimentRunner
    {
        private readonly ApplicationDbContext _context;

        private readonly MatlabService _matlab;

        public E2ExperimentRunner(
            ApplicationDbContext context,
            MatlabService matlab)
        {
            _context = context;
            _matlab = matlab;
        }

        public async Task<E2ExperimentResultDto> RunAsync(
            E2ExperimentRequestDto request)
        {


                    var result =
            new E2ExperimentResultDto
            {
                StartedAt = DateTime.Now
            };

            var totalTimer =
                System.Diagnostics.Stopwatch.StartNew();


            // CORRECCION: antes se filtraba por StudyStatus == "EXPERIMENT_READY",
            // lo que dejaba fuera estudios con otro estado (por eso E2 tenia 45 y
            // no 50). Ahora entra todo estudio que tenga features y lecturas, y
            // si el request trae StudyIds (antes se ignoraban) se respetan.
            IQueryable<MAPAStudy> query = _context
                .MAPAStudies
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.RiskFactors)
                .Include(x => x.Features)
                .Include(x => x.Readings)
                .Where(x => x.Features != null);

            if (request.StudyIds != null && request.StudyIds.Count > 0)
            {
                query = query.Where(x => request.StudyIds.Contains(x.StudyId));
            }

            List<MAPAStudy> studies = await query
                .OrderBy(x => x.StudyId)
                .ToListAsync();

            if (studies.Count == 0)
            {
                throw new InvalidOperationException(
                    "E2: no hay estudios con features para medir.");
            }

            result.StudiesSelected = studies.Count;

            List<StageMetric> metrics = new();

            for (int iteration = 1;
                 iteration <= request.Iterations;
                 iteration++)
            {
                ExperimentProfiler profiler =
                    new ExperimentProfiler(
                        experimentName: "E2",
                        scenario: "Sequential",
                        iteration: iteration,
                        coldStart: iteration == 1,
                        isAsync: false,
                        concurrency: 1);

                DiagnosisBenchmark benchmark =
                    new DiagnosisBenchmark(
                        _matlab,
                        profiler);

                foreach (MAPAStudy study in studies)
                {
                    try
                    {
                        DiagnosisData data =
                            DiagnosisDataLoader.Load(study);

                        DiagnosisBenchmarkResult diagnosis =
                            benchmark.Execute(data);

                        diagnosis.Iteration =
                            iteration;

                        result.Results.Add(diagnosis);

                        if (iteration == 1)
                        {
                            result.StudiesProcessed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(
                            $"Iteration {iteration} - Study {study.StudyId}: {ex.Message}");
                    }
                }

                metrics.AddRange(profiler.Metrics);
            }
        

            string outputFolder = ExperimentPaths.CreateOutputFolder("E2");

            CsvExporter.ExportMetrics( metrics,

                Path.Combine(
                    outputFolder,
                    "StageMetrics.csv"));

            CsvExporter.ExportStatistics(
    StatisticsService
        .CalculateAll(metrics)
        .ToList(),

                Path.Combine(
                    outputFolder,
                    "StageStatistics.csv"));

            ExportDiagnosisResults(

                result.Results,

                Path.Combine(
                    outputFolder,
                    "DiagnosisResults.csv"));

            result.OutputFolder =
                outputFolder;

            totalTimer.Stop();

            result.TotalExecutionTimeSeconds =
                totalTimer.Elapsed.TotalSeconds;

            // CORRECCION: antes dividia los estudios de UNA iteracion entre el
            // tiempo de TODAS las iteraciones (throughput subestimado ~30x).
            result.ThroughputStudiesPerSecond =
                result.TotalExecutionTimeSeconds > 0
                    ? result.Results.Count /
                      result.TotalExecutionTimeSeconds
                    : 0;

            result.MatlabFirstInitializationMs = MatlabService.FirstInitializationMs;
            result.MatlabInstancesCreated = MatlabService.InstancesCreated;

            result.FinishedAt =
                DateTime.Now;

            return result;
        }        // =====================================================
        // BUILD INPUT DATA
        // =====================================================

        // (La construccion de DiagnosisData vive ahora en DiagnosisDataLoader,
        //  compartida con E3, para que ambos experimentos usen exactamente los
        //  mismos datos de entrada.)

        // =====================================================
        // DIAGNOSIS RESULTS
        // =====================================================

        private static void ExportDiagnosisResults(
            IEnumerable<DiagnosisBenchmarkResult> results,
            string filePath)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!);

            using StreamWriter writer =
                new(filePath, false, System.Text.Encoding.UTF8);

            writer.WriteLine(
     "Iteration," +
     "StudyId," +
     "HeartRateRisk," +
     "BloodPressureRisk," +
     "PressureLoadRisk," +
     "NocturnalRisk," +
     "DiagnosisLabel," +
     "NocturnalProfile," +
     "LoadDiagnosis," +
     "HeartRateDiagnosis," +
     "TrendMatchesStored");

            foreach (DiagnosisBenchmarkResult r in results)
            {
                writer.WriteLine(string.Join(",",
                r.Iteration,
                r.StudyId,
                r.HeartRateRisk.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.BloodPressureRisk.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.PressureLoadRisk.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.NocturnalRisk.ToString(System.Globalization.CultureInfo.InvariantCulture),
                $"\"{r.DiagnosisLabel}\"",
                $"\"{r.NocturnalProfile}\"",
                $"\"{r.LoadDiagnosis}\"",
                $"\"{r.HeartRateDiagnosis}\"",
                r.TrendMatchesStored ? "true" : "false"));
            }
        }
    }
}


