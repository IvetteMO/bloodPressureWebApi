using bloodPressureWebApi.Data;
using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;
using MathWorks.MATLAB.NET.Arrays;
using Microsoft.EntityFrameworkCore;
using MySoftDiagnosisAssembly;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace bloodPressureWebApi.Experiments.E7
{
    /// <summary>
    /// E7 - Reproducibilidad y trazabilidad.
    ///
    /// CAMBIOS RESPECTO A LA VERSION ANTERIOR
    ///  1. Cada ejecucion recalcula la EXTRACCION DE CARACTERISTICAS (incluida la
    ///     red neuronal modular) y luego el diagnostico. Antes solo se repetia
    ///     el diagnostico sobre las features guardadas, asi que la ANN nunca se
    ///     ponia a prueba.
    ///  2. La firma contiene solo valores CALCULADOS (ANN, medias, ratios, FIS,
    ///     etiquetas, Framingham). Antes incluia constantes del codigo
    ///     (versiones y ConfidenceScore = 0.95), que siempre coinciden.
    ///  3. Procedencia REAL del software, medida en tiempo de ejecucion:
    ///     SHA-256 del ensamblado compilado con los modelos, version del
    ///     ensamblado MWArray, version de .NET, SO y maquina. Se guarda en
    ///     Provenance.csv y en la respuesta.
    ///  4. Resumen por estudio (ReproducibilitySummary.csv): ejecuciones y
    ///     numero de hashes distintos (1 = reproducible).
    ///  5. Tiempos separados: features (incluye ANN) y diagnostico.
    ///  6. ChangeTracker.Clear() tras cada estudio y traza de E1-B apagada
    ///     durante la corrida (ambas inflaban los tiempos).
    /// </summary>
    public sealed class E7ExperimentRunner
    {
        private readonly ApplicationDbContext _context;
        private readonly FeatureExtractionService _featureService;
        private readonly AIDiagnosisService _diagnosisService;
        private readonly E1BBackendTrace _trace;

        public E7ExperimentRunner(
            ApplicationDbContext context,
            FeatureExtractionService featureService,
            AIDiagnosisService diagnosisService,
            E1BBackendTrace trace)
        {
            _context = context;
            _featureService = featureService;
            _diagnosisService = diagnosisService;
            _trace = trace;
        }

        public async Task<E7ExperimentResultDto> RunAsync(E7ExperimentRequestDto request)
        {
            var result = new E7ExperimentResultDto
            {
                StartedAt = DateTime.UtcNow,
                Iterations = request.Iterations
            };

            var totalTimer = Stopwatch.StartNew();

            // -------------------------------------------------
            // Estudios + cargas de presion (entrada de ExtractFeatures)
            // -------------------------------------------------
            IQueryable<MAPAStudy> query = _context.MAPAStudies
                .AsNoTracking()
                .Include(s => s.Features)
                .Where(s => s.Features != null);

            if (request.StudyIds != null && request.StudyIds.Count > 0)
            {
                query = query.Where(s => request.StudyIds.Contains(s.StudyId));
            }

            var studies = await query
                .OrderBy(s => s.StudyId)
                .Select(s => new
                {
                    s.StudyId,
                    DayLoad = s.Features!.DayPressureLoad ?? 0,
                    NightLoad = s.Features!.NightPressureLoad ?? 0
                })
                .ToListAsync();

            if (studies.Count == 0)
            {
                throw new InvalidOperationException("E7: no hay estudios con features.");
            }

            result.StudiesProcessed = studies.Count;

            // -------------------------------------------------
            // Procedencia (una vez)
            // -------------------------------------------------
            result.Provenance = CaptureProvenance();

            Guid experimentId = Guid.NewGuid();
            var traces = new List<TraceRecord>();

            bool traceWasEnabled = _trace.Enabled;
            _trace.Enabled = false;

            try
            {
                for (int iteration = 1; iteration <= request.Iterations; iteration++)
                {
                    foreach (var study in studies)
                    {
                        var rec = new TraceRecord
                        {
                            ExperimentId = experimentId,
                            Iteration = iteration,
                            StudyId = study.StudyId,
                            StartedAt = DateTime.UtcNow,
                            ModelAssemblySha256 = result.Provenance.ModelAssemblySha256,
                            ModelAssemblyVersion = result.Provenance.ModelAssemblyVersion,
                            MWArrayAssemblyVersion = result.Provenance.MWArrayAssemblyVersion
                        };

                        var total = Stopwatch.StartNew();
                        try
                        {
                            var swF = Stopwatch.StartNew();
                            await _featureService.ExtractFeatures(
                                study.StudyId, study.DayLoad, study.NightLoad);
                            swF.Stop();

                            var swD = Stopwatch.StartNew();
                            await _diagnosisService.RunDiagnosis(study.StudyId, includeFramingham: false);
                            swD.Stop();

                            var feature = await _context.MAPAFeatures.AsNoTracking()
                                .FirstAsync(x => x.StudyId == study.StudyId);
                            var diagnosis = await _context.AIDiagnosis.AsNoTracking()
                                .FirstAsync(x => x.StudyId == study.StudyId);

                            rec.Signature = ResultSignatureBuilder.BuildOutputSignature(feature, diagnosis);
                            rec.ResultHash = ResultSignatureBuilder.ComputeHash(rec.Signature);
                            rec.FeaturesMs = swF.Elapsed.TotalMilliseconds;
                            rec.DiagnosisMs = swD.Elapsed.TotalMilliseconds;
                            rec.Success = true;
                        }
                        catch (Exception ex)
                        {
                            rec.Success = false;
                            rec.ErrorMessage = ex.Message;
                        }
                        finally
                        {
                            total.Stop();
                            rec.TotalMs = total.Elapsed.TotalMilliseconds;
                            rec.FinishedAt = DateTime.UtcNow;
                            _context.ChangeTracker.Clear();
                        }

                        traces.Add(rec);
                    }
                }
            }
            finally
            {
                _trace.Enabled = traceWasEnabled;
            }

            // -------------------------------------------------
            // Resumen de reproducibilidad por estudio
            // -------------------------------------------------
            var perStudy = traces
                .GroupBy(t => t.StudyId)
                .Select(g => new StudyReproducibility
                {
                    StudyId = g.Key,
                    Executions = g.Count(),
                    Successful = g.Count(x => x.Success),
                    DistinctHashes = g.Where(x => x.Success).Select(x => x.ResultHash).Distinct().Count(),
                    ReferenceHash = g.Where(x => x.Success).Select(x => x.ResultHash).FirstOrDefault() ?? ""
                })
                .OrderBy(x => x.StudyId)
                .ToList();

            result.SuccessfulExecutions = traces.Count(x => x.Success);
            result.FailedExecutions = traces.Count(x => !x.Success);
            result.StudiesFullyReproducible = perStudy.Count(x =>
                x.Successful == x.Executions && x.DistinctHashes == 1);
            result.DistinctHashesOverall = traces.Where(x => x.Success)
                .Select(x => x.ResultHash).Distinct().Count();

            // -------------------------------------------------
            // Exportar
            // -------------------------------------------------
            string outputFolder = ExperimentPaths.CreateOutputFolder("E7");
            result.OutputFolder = outputFolder;

            if (request.ExportCsv)
            {
                TraceExporter.Export(traces, Path.Combine(outputFolder, "TraceRecords.csv"));
                ExportSummary(perStudy, Path.Combine(outputFolder, "ReproducibilitySummary.csv"));
                ExportProvenance(result.Provenance, Path.Combine(outputFolder, "Provenance.csv"));
            }

            totalTimer.Stop();
            result.TotalExecutionTimeSeconds = totalTimer.Elapsed.TotalSeconds;
            result.FinishedAt = DateTime.UtcNow;
            return result;
        }

        // =====================================================
        // PROCEDENCIA
        // =====================================================

        private static E7Provenance CaptureProvenance()
        {
            var modelAssembly = typeof(BPLevelAPI).Assembly;
            var mwArrayAssembly = typeof(MWNumericArray).Assembly;

            string modelPath = modelAssembly.Location;
            string modelHash = File.Exists(modelPath)
                ? ResultSignatureBuilder.ComputeFileHash(modelPath)
                : "UNAVAILABLE";

            return new E7Provenance
            {
                ModelAssemblyName = modelAssembly.GetName().Name ?? "",
                ModelAssemblyVersion = modelAssembly.GetName().Version?.ToString() ?? "",
                ModelAssemblySha256 = modelHash,
                ModelAssemblyPath = modelPath,
                MWArrayAssemblyVersion = mwArrayAssembly.GetName().Version?.ToString() ?? "",
                MatlabRuntimeEnvironment = Environment.GetEnvironmentVariable("MATLAB_RUNTIME_VERSION") ?? "",
                DotNetRuntime = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                MatlabServiceInitializationMs = MatlabService.FirstInitializationMs,
                CapturedAtUtc = DateTime.UtcNow
            };
        }

        // =====================================================
        // EXPORT
        // =====================================================

        private static string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        private static void ExportSummary(IEnumerable<StudyReproducibility> rows, string path)
        {
            using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
            w.WriteLine("StudyId,Executions,Successful,DistinctHashes,Reproducible,ReferenceHash");
            foreach (var r in rows)
            {
                w.WriteLine(string.Join(",", r.StudyId, r.Executions, r.Successful, r.DistinctHashes,
                    (r.Successful == r.Executions && r.DistinctHashes == 1) ? "true" : "false",
                    Q(r.ReferenceHash)));
            }
        }

        private static void ExportProvenance(E7Provenance p, string path)
        {
            using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
            var inv = CultureInfo.InvariantCulture;
            w.WriteLine("Property,Value");
            w.WriteLine($"ModelAssemblyName,{Q(p.ModelAssemblyName)}");
            w.WriteLine($"ModelAssemblyVersion,{Q(p.ModelAssemblyVersion)}");
            w.WriteLine($"ModelAssemblySha256,{Q(p.ModelAssemblySha256)}");
            w.WriteLine($"ModelAssemblyPath,{Q(p.ModelAssemblyPath)}");
            w.WriteLine($"MWArrayAssemblyVersion,{Q(p.MWArrayAssemblyVersion)}");
            w.WriteLine($"MatlabRuntimeEnvironment,{Q(p.MatlabRuntimeEnvironment)}");
            w.WriteLine($"DotNetRuntime,{Q(p.DotNetRuntime)}");
            w.WriteLine($"OperatingSystem,{Q(p.OperatingSystem)}");
            w.WriteLine($"ProcessArchitecture,{Q(p.ProcessArchitecture)}");
            w.WriteLine($"MachineName,{Q(p.MachineName)}");
            w.WriteLine($"ProcessorCount,{p.ProcessorCount}");
            w.WriteLine($"MatlabServiceInitializationMs,{p.MatlabServiceInitializationMs?.ToString("R", inv) ?? ""}");
            w.WriteLine($"CapturedAtUtc,{Q(p.CapturedAtUtc.ToString("O", inv))}");
        }
    }
}
