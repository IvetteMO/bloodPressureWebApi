using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Experiments;
using bloodPressureWebApi.DTOs.Import;
using bloodPressureWebApi.Experiments.Core;
using bloodPressureWebApi.Experiments.E2;
using bloodPressureWebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Services
{
    public class E1ExperimentRunner
    {
        private readonly ApplicationDbContext _context;

        private readonly MAPAImportService _importService;

        private readonly FeatureExtractionService _featureService;

        private readonly AIDiagnosisService _diagnosisService;

        private readonly MAPAMetadataReaderService _metadataReader;

        private readonly E1ValidationService _validationService;

        private readonly E1StatisticalAnalysisService _statisticalAnalysisService;

        public E1ExperimentRunner(
            ApplicationDbContext context,
            MAPAImportService importService,
            FeatureExtractionService featureService,
            AIDiagnosisService diagnosisService,
            MAPAMetadataReaderService metadataReader,
            E1ValidationService validationService,
            E1StatisticalAnalysisService statisticalAnalysisService
            )
        {
            _context = context;
            _importService = importService;
            _featureService = featureService;
            _diagnosisService = diagnosisService;
            _metadataReader = metadataReader;
            _validationService = validationService;
            _statisticalAnalysisService = statisticalAnalysisService;
        }

        public async Task<ExperimentalImportResultDto> ImportDataset(
     ExperimentalImportRequestDto request)
        {
            ValidateRequest(request);

            List<string> files =
                GetExcelFiles(request.FolderPath);

            Guid batchId =
                Guid.NewGuid();

            ExperimentalImportResultDto result =
                CreateBatch(batchId, files.Count);

            ExperimentProfiler profiler =
                new ExperimentProfiler(
                    experimentName: "E1",
                    scenario: "Sequential",
                    iteration: 1,
                    coldStart: true,
                    isAsync: false,
                    concurrency: 1);

            foreach (string file in files)
            {
                ExperimentalStudyResultDto study =
                    await ProcessStudyAsync(
                        batchId,
                        request,
                        file,
                        profiler);

                result.Studies.Add(study);

                switch (study.Status)
                {
                    case "ImportedAndValidated":
                        result.Imported++;
                        break;

                    case "Skipped":
                        result.Duplicates++;
                        break;

                    default:
                        result.Failed++;
                        break;
                }
            }

            string outputFolder =
                Path.Combine(
                    request.FolderPath,
                    "ExperimentResults");

            CsvExporter.ExportMetrics(
                profiler.Metrics,
                Path.Combine(
                    outputFolder,
                    "StageMetrics.csv"));

            var statistics =
     profiler.BuildStatistics();

            CsvExporter.ExportStatistics(
    statistics,
    Path.Combine(
        outputFolder,
        "StageStatistics.csv"));


            await _statisticalAnalysisService
    .ExportComparisonDatasetAsync(
        batchId,
        Path.Combine(
            outputFolder,
            "E1_ComparisonDataset.csv"));


            result.FinishedAt = DateTime.Now;

           

            return result;
        }



        private static void ValidateRequest(
    ExperimentalImportRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FolderPath))
                throw new ArgumentException("FolderPath is required.");

            if (!Directory.Exists(request.FolderPath))
                throw new DirectoryNotFoundException(request.FolderPath);
        }

        private static List<string> GetExcelFiles(
    string folder)
        {
            return Directory
                .GetFiles(
                    folder,
                    "*.xlsx",
                    SearchOption.TopDirectoryOnly)
                .Where(x =>
                    !Path.GetFileName(x)
                        .StartsWith("~$"))
                .OrderBy(x => x)
                .ToList();
        }

        private static ExperimentalImportResultDto CreateBatch(
    Guid batchId,
    int filesFound)
        {
            return new ExperimentalImportResultDto
            {
                BatchId = batchId,
                StartedAt = DateTime.Now,
                FilesFound = filesFound
            };
        }

        // PARTE 2

        private async Task<ExperimentalStudyResultDto> ProcessStudyAsync(
     Guid batchId,
     ExperimentalImportRequestDto request,
     string filePath,
     ExperimentProfiler profiler)
        {
            ExperimentalStudyResultDto result =
                new()
                {
                    FileName = Path.GetFileName(filePath)
                };

            try
            {
                MAPAMetadataDto metadata;

                using (profiler.Stage("ReadMetadata"))
                {
                    metadata = ReadMetadata(filePath);
                }

                result.ExternalPatientCode =
                    metadata.ExternalPatientCode;

                Patients patient;

                using (profiler.Stage("GetOrCreatePatient"))
                {
                    patient =
                        await GetOrCreatePatientAsync(metadata);
                }

                result.PatientId =
                    patient.PatientId;

                MAPAStudy study;

                using (profiler.Stage("CreateStudy"))
                {
                    study =
                        await CreateStudyAsync(
                            patient,
                            metadata,
                            request);
                }

                result.StudyId =
                    study.StudyId;

                using (profiler.Stage("Pipeline"))
                {
                    await ExecutePipelineAsync(
    batchId,
    study,
    metadata,
    filePath,
    profiler,
    false);
                }

                using (profiler.Stage("CompleteStudy"))
                {
                    await CompleteStudyAsync(study);
                }

                result.Status =
                    "ImportedAndValidated";
            }
            catch (InvalidOperationException ex)
            {
                result.Status =
                    "Skipped";

                result.Error =
                    ex.ToString();
            }
            catch (Exception ex)
            {
                result.Status =
                    "Failed";

                result.Error =
                    ex.ToString();
            }

            return result;
        }

        private MAPAMetadataDto ReadMetadata(
    string filePath)
        {
            return _metadataReader
                .Read(filePath);
        }



        private async Task<Patients> GetOrCreatePatientAsync(
    MAPAMetadataDto metadata)
        {
            Patients? patient =
                await _context
                    .Patients
                    .FirstOrDefaultAsync(x =>
                        x.ExternalPatientCode ==
                        metadata.ExternalPatientCode);

            if (patient != null)
                return patient;

            patient = new Patients
            {
                ExternalPatientCode =
                    metadata.ExternalPatientCode,

                FirstName =
                    "De-identified",

                LastName =
                    metadata.ExternalPatientCode,

                CreatedAt =
                    DateTime.Now
            };

            _context.Patients.Add(patient);

            await _context.SaveChangesAsync();

            return patient;
        }

        private async Task<MAPAStudy> CreateStudyAsync(
    Patients patient,
    MAPAMetadataDto metadata,
    ExperimentalImportRequestDto request)
        {
            MAPAStudy study =
                new()
                {
                    PatientId =
                        patient.PatientId,

                    ExternalStudyCode =
                        metadata.ExternalPatientCode,

                    StudyDate =
                        DateTime.Now,

                    AwakeTime =
                        request.DefaultAwakeTime,

                    SleepTime =
                        request.DefaultSleepTime,

                    CustomSchedule =
                        false,

                    ExcelFileName =
                        metadata.SourceFileName,

                    StudyStatus =
                        "IMPORTING",

                    ProcessingStatus =
                        "E1_IMPORT",

                    Version =
                        1,

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.Now
                };

            _context.MAPAStudies.Add(study);

            await _context.SaveChangesAsync();

            bool exists =
                await _context
                    .MAPAStudies
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.StudyId ==
                        study.StudyId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"Study {study.StudyId} was created but could not be found before import.");
            }

            return study;
        }

        private async Task CompleteStudyAsync(
    MAPAStudy study)
        {
            study.StudyStatus =
                "EXPERIMENT_READY";

            study.ProcessingStatus =
                "E1_VALIDATED";

            await _context.SaveChangesAsync();
        }

        private async Task ExecutePipelineAsync(
    Guid batchId,
    MAPAStudy study,
    MAPAMetadataDto metadata,
    string filePath,
    ExperimentProfiler profiler,
    bool detailedProfiling)
        {
            await using FileStream stream =
                new(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

            IFormFile formFile =
                new FormFile(
                    stream,
                    0,
                    stream.Length,
                    "file",
                    Path.GetFileName(filePath));

            // =====================================================
            // E1 - GLOBAL PIPELINE
            // =====================================================
            if (!detailedProfiling)
            {
                var loads =
                    await _importService
                        .ImportStudyFile(
                            formFile,
                            study.StudyId);

                await _featureService
                    .ExtractFeatures(
                        study.StudyId,
                        loads.DayPressureLoad,
                        loads.NightPressureLoad);

                await _diagnosisService
                    .RunDiagnosis(study.StudyId);

                await _validationService
                    .ValidateStudy(
                        batchId,
                        study.StudyId,
                        metadata);

                return;
            }

            // =====================================================
            // E2 - DETAILED PIPELINE
            // =====================================================

            PressureLoadDto pressureLoads;

            using (profiler.Stage(DiagnosisStageNames.ImportStudy))
            {
                pressureLoads =
                    await _importService
                        .ImportStudyFile(
                            formFile,
                            study.StudyId);
            }

            using (profiler.Stage(DiagnosisStageNames.ExtractFeatures))
            {
                await _featureService
                    .ExtractFeatures(
                        study.StudyId,
                        pressureLoads.DayPressureLoad,
                        pressureLoads.NightPressureLoad);
            }

            //
            // Por ahora medimos el diagnóstico completo.
            // Posteriormente instrumentaremos internamente
            // HeartRate, BPLevel, BPLoad,
            // NocturnalBP y SaveDiagnosis.
            //
            using (profiler.Stage("RunDiagnosis"))
            {
                await _diagnosisService
                    .RunDiagnosis(study.StudyId);
            }

            using (profiler.Stage(DiagnosisStageNames.Validation))
            {
                await _validationService
                    .ValidateStudy(
                        batchId,
                        study.StudyId,
                        metadata);
            }
        }
    }
    
}