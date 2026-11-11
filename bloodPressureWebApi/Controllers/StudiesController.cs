using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Shared;
using bloodPressureWebApi.DTOs.Studies;
using bloodPressureWebApi.Models;
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
        public async Task<IActionResult> GetFullStudy(int studyId)
        {
            var study =
                await _context.MAPAStudies

                .AsNoTracking()

                .Include(x => x.Patient)

                .Include(x => x.Readings)

                .Include(x => x.RiskFactors)

                .Include(x => x.Features)

                .Include(x => x.Diagnosis)

                .FirstOrDefaultAsync(x =>
                    x.StudyId == studyId);

            if (study == null)
            {
                return NotFound(new
                {
                    message = "Study not found"
                });
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

                // =====================================
                // PATIENT
                // =====================================

                patient = new
                {
                    patientId =
                        study.Patient.PatientId,

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
        // UPLOAD
        // =====================================================

        [HttpPost("upload/{studyId}")]
        public async Task<IActionResult> UploadStudyFile(int studyId,IFormFile file)
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
                // AI
                // =====================================

                await _diagnosisService
                    .RunDiagnosis(
                        studyId);

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,

                    studyId =
                        studyId,

                    message =
                        "Study imported successfully"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    success = false,

                    message =
                        ex.Message,

                    inner =
                        ex.InnerException?.Message
                });
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