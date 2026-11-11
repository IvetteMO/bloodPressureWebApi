using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Diagnosis;
using bloodPressureWebApi.Services;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class DiagnosisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly MatlabService _matlab;

        public DiagnosisController(
            ApplicationDbContext context,
            MatlabService matlab)
        {
            _context = context;

            _matlab = matlab;
        }

        // =====================================================
        // GET DIAGNOSIS BY STUDY
        // =====================================================

        [HttpGet("study/{studyId}")]
        public async Task<IActionResult> GetDiagnosis(int studyId)
        {
            var diagnosis =
                await _context.AIDiagnosis

                .AsNoTracking()

                .Where(x =>
                    x.StudyId == studyId)

                .Select(x => new
                {
                    x.DiagnosisId,

                    x.StudyId,

                    x.FuzzyBPRisk,

                    x.FuzzyLoadC,

                    x.FuzzyNocturnalC,

                    x.FuzzyHeartRC,

                    x.FinalRiskLevel,

                    x.HeartAge,

       
                    x.CreatedAt
                })

                .FirstOrDefaultAsync();

            if (diagnosis == null)
            {
                return NotFound(new
                {
                    message =
                        "Diagnosis not found"
                });
            }

            return Ok(diagnosis);
        }

        // =====================================================
        // GET ALL DIAGNOSIS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetAllDiagnosis()
        {
            var diagnosis =
                await _context.AIDiagnosis

                .AsNoTracking()

                .Select(x => new
                {
                    x.DiagnosisId,

                    x.StudyId,

                    x.FuzzyBPRisk,

                    x.FuzzyLoadC,

                    x.FuzzyNocturnalC,

                    x.FinalRiskLevel,

                    x.HeartAge,


                })

                .ToListAsync();

            return Ok(diagnosis);
        }

        // =====================================================
        // QUICK DIAGNOSIS
        // =====================================================

        [HttpPost("quick")]
        public IActionResult QuickDiagnosis(
            [FromBody]
            QuickDiagnosisRequestDto request)
        {
            double avgSys =
                (
                    request.Systolic1 +
                    request.Systolic2 +
                    request.Systolic3
                ) / 3.0;

            double avgDia =
                (
                    request.Diastolic1 +
                    request.Diastolic2 +
                    request.Diastolic3
                ) / 3.0;

            double avgPulse =
                (
                    request.Pulse1 +
                    request.Pulse2 +
                    request.Pulse3
                ) / 3.0;

            double bpRisk =
                _matlab.BPLevel(
                    avgSys,
                    avgDia);

            double hrRisk =
                _matlab.HeartRate(
                    request.Age,
                    avgPulse);

            double hypertensionRisk =
                _matlab.FHS(
                    request.Age,
                    1,
                    25,
                    avgSys,
                    avgDia,
                    0,
                    0);

            double[] cvd =
                _matlab.CVDRisk(
                    1,
                    request.Age,
                    avgSys,
                    0,
                    0,
                    25,
                    0);

            return Ok(new
            {
                AverageSystolic =
                    avgSys,

                AverageDiastolic =
                    avgDia,

                AveragePulse =
                    avgPulse,

                FuzzyHypertensionRisk =
                    bpRisk,

                HeartRateRisk =
                    hrRisk,

                HypertensionRisk =
                    hypertensionRisk,

                FuzzyCardiovascularRisk =
                    cvd.Length > 0
                    ? cvd[0]
                    : 0
            });
        }

        // =====================================================
        // BP LEVEL
        // =====================================================

        [HttpPost("bp-level")]
        public IActionResult BPLevel(
            [FromBody]
            BPLevelRequestDto request)
        {
            double risk =
                _matlab.BPLevel(
                    request.Systolic,
                    request.Diastolic);

            return Ok(new
            {
                Risk = risk
            });
        }

        // =====================================================
        // HEART RATE
        // =====================================================

        [HttpPost("heart-rate")]
        public IActionResult HeartRate(
            [FromBody]
            HeartRateRequestDto request)
        {
            double risk =
                _matlab.HeartRate(
                    request.Age,
                    request.Pulse);

            return Ok(new
            {
                Risk = risk
            });
        }

        // =====================================================
        // NOCTURNAL
        // =====================================================

        [HttpPost("nocturnal")]
        public IActionResult Nocturnal(
            [FromBody]
            NocturnalRequestDto request)
        {
            double sysRatio =
                request.NightSystolic
                /
                request.DaySystolic;

            double diaRatio =
                request.NightDiastolic
                /
                request.DayDiastolic;

            double risk =
                _matlab.NocturnalBP(
                    sysRatio,
                    diaRatio);

            string profile =
                "DIPPER";

            if (
                sysRatio >= 0.90
                &&
                sysRatio <= 1.0)
            {
                profile =
                    "NON-DIPPER";
            }

            if (sysRatio > 1.0)
            {
                profile =
                    "REVERSE DIPPER";
            }

            if (sysRatio < 0.80)
            {
                profile =
                    "EXTREME DIPPER";
            }

            return Ok(new
            {
                SystolicRatio =
                    sysRatio,

                DiastolicRatio =
                    diaRatio,

                Risk =
                    risk,

                Profile =
                    profile
            });
        }

        // =====================================================
        // CVD RISK
        // =====================================================

        [HttpPost("cvd-risk")]public IActionResult CVDRisk([FromBody]CVDRiskRequestDto request)
        {
            double[] result =
                _matlab.CVDRisk(
                    request.Sex,
                    request.Age,
                    request.Systolic,
                    request.Smoker,
                    request.Diabetes,
                    request.BMI,
                    request.Treatment);

            return Ok(new
            {
                CardiovascularRisk =
                    result.Length > 0
                    ? result[0]
                    : 0,

                            HeartAge =
                    result.Length > 1
                    ? result[1]
                    : 0
            });
        }

        // =====================================================
        // FHS
        // =====================================================

        [HttpPost("fhs")]
        public IActionResult FHS(
            [FromBody]
            FHSRequestDto request)
        {
            double hypertensionRisk =
                _matlab.FHS(
                    request.Age,
                    request.Sex,
                    request.BMI,
                    request.Systolic,
                    request.Diastolic,
                    request.Smoker,
                    request.Parents);

            return Ok(new
            {
                Hypertension4YearRisk =
                    hypertensionRisk
            });
        }
    }
}