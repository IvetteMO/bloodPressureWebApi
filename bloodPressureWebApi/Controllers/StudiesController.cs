using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Shared;
using bloodPressureWebApi.DTOs.Studies;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class StudiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly MAPAImportService _importService;

        private readonly FeatureExtractionService _featureService;

        private readonly AIDiagnosisService _diagnosisService;

        public StudiesController(
            ApplicationDbContext context,
            MAPAImportService importService,
            FeatureExtractionService featureService,
            AIDiagnosisService diagnosisService)
        {
            _context =
                context;

            _importService =
                importService;

            _featureService =
                featureService;

            _diagnosisService =
                diagnosisService;
        }

        // =====================================================
        // GET ALL STUDIES
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetStudies()
        {
            var studies =
                await _context.MAPAStudies

                .AsNoTracking()

                .Select(x => new
                {
                    x.StudyId,

                    x.ExcelFileName,

                    x.TotalReadings,

                    x.StudyDate,

                    x.StudyStatus
                })

                .ToListAsync();

            return Ok(studies);
        }

        // =====================================================
        // GET SINGLE STUDY
        // =====================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudy(
            int id)
        {
            var study =
                await _context.MAPAStudies

                .AsNoTracking()

                .Include(x => x.Features)

                .Include(x => x.Diagnosis)

                .FirstOrDefaultAsync(x =>
                    x.StudyId == id);

            if (study == null)
            {
                return NotFound(new
                {
                    message =
                        "Study not found"
                });
            }

            return Ok(study);
        }

        [HttpGet("full/{studyId}")]
        public async Task<IActionResult> GetFullStudy(
            int studyId,
            [FromServices] IHostEnvironment environment)
        {
            var study =
                await _context.MAPAStudies

                .AsNoTracking()

                .Include(x => x.Patient)

                .Include(x => x.Readings)

                .Include(x => x.RiskFactors)

                .Include(x => x.Features)

                .Include(x => x.Diagnosis)

                // Lecturas x factores de riesgo en un solo JOIN multiplica
                // filas (producto cartesiano): mejor una consulta por coleccion.
                .AsSplitQuery()

                .FirstOrDefaultAsync(x =>
                    x.StudyId == studyId);

            if (study == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Estudio no encontrado",
                    detail: $"No existe el estudio {studyId}.");
            }

            return Ok(new
            {
                // =====================================
                // STUDY
                // =====================================

                studyId =
                    study.StudyId,

                patientId =
                    study.PatientId,

                studyDate =
                    study.StudyDate,

                awakeTime =
                    study.AwakeTime,

                sleepTime =
                    study.SleepTime,

                excelFileName =
                    study.ExcelFileName,

                totalReadings =
                    study.TotalReadings,

                studyStatus =
                    study.StudyStatus,

                // Diagnostico en proceso / fallido (ver GET {id}/status)
                processingStatus =
                    study.ProcessingStatus,

                processingError =
                    FormatProcessingError(study, environment),

                // =====================================
                // PATIENT
                // =====================================

                patient = new
                {
                    patientId =
                        study.Patient!.PatientId,

                    firstName =
                        study.Patient.FirstName,

                    lastName =
                        study.Patient.LastName,

                    age =
                        study.Patient.BirthDate,

                    sex =
                        study.Patient.Sex,

                    phoneNumber =
                        study.Patient.PhoneNumber,

                    email =
                        study.Patient.Email,

                    notes =
                        study.Patient.Notes
                },

                // =====================================
                // READINGS
                // =====================================

                readings =
                    study.Readings.Select(r => new
                    {
                        readingId =
                            r.ReadingId,

                        readingDateTime =
                            r.ReadingDateTime,

                        systolic =
                            r.Systolic,

                        diastolic =
                            r.Diastolic,

                        pulse =
                            r.Pulse,

                        period =
                            r.Period
                    }),

                // =====================================
                // RISK FACTORS
                // =====================================

                riskFactors =
                    study.RiskFactors.Select(r => new
                    {
                        riskFactorId =
                            r.RiskFactorId,

                        studyId =
                            r.StudyId,

                        weight =
                            r.Weight,

                        height =
                            r.Height,

                        bmi =
                            r.BMI,

                        smoking =
                            r.Smoking,

                        diabetes =
                            r.Diabetes,

                        hypertensionTreatment =
                            r.HypertensionTreatment,

                        parentalHypertension =
                            r.ParentalHypertension
                    }),

                // =====================================
                // FEATURES
                // =====================================

                features =
                    study.Features == null
                    ? null
                    : new
                    {
                        systolicTrend =
                            study.Features.SystolicTrend,

                        diastolicTrend =
                            study.Features.DiastolicTrend,

                        pulseTrend =
                            study.Features.PulseTrend,

                        systolicNightRatio =
                            study.Features.SystolicNightRatio,

                        diastolicNightRatio =
                            study.Features.DiastolicNightRatio,

                        systolicDayMean =
                            study.Features.DaySystolicMean,

                        diastolicDayMean =
                            study.Features.DayDiastolicMean,

                        systolicNightMean =
                            study.Features.NightSystolicMean,

                        diastolicNightMean =
                            study.Features.NightDiastolicMean,

                        dippingPercent =
                            study.Features.DippingPercent,

                        dayPressureLoad =
                            study.Features.DayPressureLoad,

                        nightPressureLoad =
                            study.Features.NightPressureLoad
                    },

                // =====================================
                // DIAGNOSIS
                // =====================================

                diagnosis =
                    study.Diagnosis == null
                    ? null
                    : new
                    {
                        hypertension4YearRisk =
                            study.Diagnosis.Hypertension4YearRisk,

                        finalRiskLevel =
                            study.Diagnosis.FinalRiskLevel,

                        nocturnalProfile =
                            study.Diagnosis.NocturnalProfile,

                        loadDiagnosis =
                            study.Diagnosis.LoadDiagnosis,

                        heartRateDiagnosis =
                            study.Diagnosis.HeartRateDiagnosis,

                        cardiovascularRisk =
                             study.Diagnosis.CardiovascularRisk,

                         heartAge = study.Diagnosis.HeartAge,


                        // =====================================
                         // FUZZY OUTPUTS
                         // =====================================

                                    fuzzyBPRisk =
                        study.Diagnosis.FuzzyBPRisk,

                                    fuzzyLoadC =
                        study.Diagnosis.FuzzyLoadC,

                                    fuzzyNocturnalC =
                        study.Diagnosis.FuzzyNocturnalC,

                                    fuzzyHeartRC =
                        study.Diagnosis.FuzzyHeartRC,

                                    // =====================================
                                    // AI INFO
                                    // =====================================

                                    confidenceScore =
                        study.Diagnosis.ConfidenceScore,

                                    explanations =
                        study.Diagnosis.Explanations,

                                    modelVersion =
                        study.Diagnosis.ModelVersion
                    }
            });
        }

        // =====================================================
        // CREATE STUDY
        // =====================================================

        [HttpPost]

        public async Task<IActionResult>CreateStudy(CreateStudyDto dto)
        {
            var study =
                new MAPAStudy
                {
                    PatientId =
                        dto.PatientId,

                    AwakeTime =
                        dto.AwakeTime,

                    SleepTime =
                        dto.SleepTime,

                    CustomSchedule =
                        dto.AwakeTime != null
                        &&
                        dto.SleepTime != null,

                    StudyDate =
                        DateTime.Now,

                    StudyStatus =
                        "CREATED",

                    CreatedAt =
                        DateTime.Now
                };

            _context.MAPAStudies
                .Add(study);

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                success = true,

                studyId =
                    study.StudyId
            });
        }
        // =====================================================
        // PROCESSING STATUS
        //
        // GET api/Studies/{id}/status
        // Respuesta minima para que el frontend consulte cada pocos
        // segundos mientras el diagnostico se recalcula.
        // =====================================================

        [HttpGet("{id:int}/status")]
        [ProducesResponseType<StudyProcessingStatusResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProcessingStatus(
            int id,
            [FromServices] IHostEnvironment environment,
            CancellationToken cancellationToken)
        {
            var study = await _context.MAPAStudies
                .AsNoTracking()
                .Where(x => x.StudyId == id)
                .Select(x => new MAPAStudy
                {
                    StudyId = x.StudyId,
                    ProcessingStatus = x.ProcessingStatus,
                    ProcessingError = x.ProcessingError
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (study is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Estudio no encontrado",
                    detail: $"No existe el estudio {id}.");
            }

            return Ok(new StudyProcessingStatusResponse(
                study.StudyId,
                study.ProcessingStatus,
                StudyProcessingStatus.IsInProgress(study.ProcessingStatus),
                study.ProcessingStatus == StudyProcessingStatus.Failed,
                FormatProcessingError(study, environment)));
        }

        // El worker guarda ex.Message tal cual (puede traer detalles de
        // MATLAB o SQL). Solo se muestra completo en Development.
        private static string? FormatProcessingError(
            MAPAStudy study,
            IHostEnvironment environment)
        {
            if (study.ProcessingStatus != StudyProcessingStatus.Failed)
            {
                return null;
            }

            return environment.IsDevelopment()
                ? study.ProcessingError
                : "The diagnosis could not be recalculated. Try again or contact support.";
        }

        // =====================================================
        // CREATE WITH FILE (multipart/form-data)
        //
        // Estudio + factores de riesgo + Excel para un paciente existente,
        // en una sola transaccion: si el Excel falla, no queda un estudio
        // vacio.
        // =====================================================

        [HttpPost("with-file")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [ProducesResponseType<StudyIntakeResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateStudyWithFile(
            [FromForm] CreateStudyWithFileRequest request,
            [FromServices] StudyIntakeService intakeService,
            CancellationToken cancellationToken)
        {
            var result = await intakeService.AddStudyAsync(
                request,
                cancellationToken);

            if (result is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Paciente no encontrado",
                    detail: $"No existe el paciente {request.PatientId}.");
            }

            return CreatedAtAction(
                nameof(GetStudy),
                new { id = result.StudyId },
                result);
        }

        // =====================================================
        // UPLOAD
        // =====================================================

        [HttpPost("upload/{studyId}")]
        public async Task<IActionResult> UploadStudyFile(
            int studyId,
            IFormFile file,
            [FromServices] DiagnosisQueue queue)
        {
            using var transaction =
                await _context
                    .Database
                    .BeginTransactionAsync();

            try
            {
                if (file == null)
                {
                    return BadRequest(new
                    {
                        success = false,

                        message =
                            "No file uploaded"
                    });
                }

                // =====================================
                // IMPORT
                // =====================================

                var loads =
                    await _importService
                        .ImportStudyFile(
                            file,
                            studyId);

                // =====================================
                // FEATURES
                // =====================================

                await _featureService
                    .ExtractFeatures(
                        studyId,
                        loads.DayPressureLoad,
                        loads.NightPressureLoad);

                // =====================================
                // AI (en la cola, como todas las rutas)
                // El archivo ya es valido: lo que falte es MATLAB, y eso
                // lo resuelve el worker con reintentos y estado visible.
                // =====================================

                var study = await _context.MAPAStudies
                    .FirstAsync(x => x.StudyId == studyId);

                study.ProcessingStatus = StudyProcessingStatus.Queued;
                study.ProcessingError = null;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await queue.EnqueueAsync(new DiagnosisJob { StudyId = studyId });

                return Accepted(new
                {
                    success = true,

                    studyId =
                        studyId,

                    processingStatus =
                        study.ProcessingStatus,

                    message =
                        "Study imported; diagnosis queued"
                });
            }
            catch
            {
                // La excepcion la convierte en ProblemDetails el
                // GlobalExceptionHandler (sin exponer detalles internos).
                await transaction.RollbackAsync();

                throw;
            }
        }

        // =====================================================
        // PATIENT STUDIES
        // =====================================================

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult>GetStudiesByPatient(int patientId)
        {
            var studies =
                await _context.MAPAStudies

                .Include(x => x.Features)

                .Include(x => x.Diagnosis)

                .Where(x =>
                    x.PatientId == patientId)

                .OrderByDescending(x =>
                    x.CreatedAt)

                .ToListAsync();

            return Ok(studies);
        }

        [HttpDelete("{studyId}")]
        public async Task<IActionResult>DeleteStudy(int studyId)
        {
            var study =
                await _context
                    .MAPAStudies

                    .FirstOrDefaultAsync(x =>
                        x.StudyId == studyId);

            if (study == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Study not found"
                });
            }

            _context
                .MAPAStudies
                .Remove(study);

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }
    }
}