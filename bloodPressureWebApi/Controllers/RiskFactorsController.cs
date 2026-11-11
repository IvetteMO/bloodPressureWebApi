using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Studies;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class RiskFactorsController
        : ControllerBase
    {
        private readonly
            ApplicationDbContext _context;

        private readonly DiagnosisQueue _queue;

        public RiskFactorsController(ApplicationDbContext context, DiagnosisQueue queue)
        {
            _context = context;
            _queue = queue; 
        }

        // =====================================
        // CREATE
        // =====================================

        [HttpPost]

        public async Task<IActionResult>
    Create([FromBody] CreateRiskFactorDto dto)
        {
            var risk =
                new PatientRiskFactor
                {
                    StudyId =
                        dto.StudyId,

                    Weight =
                        dto.Weight,

                    Height =
                        dto.Height,

                    BMI =
                        dto.BMI,

                    Smoking =
                        dto.Smoking,

                    Diabetes =
                        dto.Diabetes,

                    HypertensionTreatment =
                        dto.HypertensionTreatment,

                    ParentalHypertension =
                        dto.ParentalHypertension,

                    CreatedAt =
                        DateTime.Now
                };

            // =====================================
            // SAVE
            // =====================================

            _context
                .PatientRiskFactors
                .Add(risk);

            await _context
                .SaveChangesAsync();

            // =====================================
            // RECALCULATE DIAGNOSIS
            // =====================================

           /* await _queue
                .EnqueueAsync(
                    new DiagnosisJob
                    {
                        StudyId =
                            dto.StudyId
                    });*/

            return Ok(risk);
        }
        // =====================================
        // GET BY STUDY
        // =====================================

        [HttpGet("study/{studyId}")]

        public async Task<IActionResult>
            GetByStudy(int studyId)
        {
            var risk =
                await _context
                    .PatientRiskFactors

                    .FirstOrDefaultAsync(
                        x =>
                            x.StudyId
                            == studyId);

            if (risk == null)
            {
                return NotFound();
            }

            return Ok(risk);
        }



        [HttpPut("{id}")]

        public async Task<IActionResult>
    Update(
        int id,

        PatientRiskFactor updated)
        {
            var risk =
                await _context
                    .PatientRiskFactors

                    .FirstOrDefaultAsync(
                        x =>
                            x.RiskFactorId
                            == id);

            if (risk == null)
            {
                return NotFound();
            }

            // =====================================
            // UPDATE
            // =====================================

            risk.Weight =
                updated.Weight;

            risk.Height =
                updated.Height;

            risk.BMI =
                updated.BMI;

            risk.Smoking =
                updated.Smoking;

            risk.Diabetes =
                updated.Diabetes;

            risk.HypertensionTreatment =
                updated
                    .HypertensionTreatment;

            risk.ParentalHypertension =
                updated
                    .ParentalHypertension;

            // =====================================
            // SAVE
            // =====================================

            await _context
                .SaveChangesAsync();

            // =====================================
            // RECALCULATE
            // =====================================

            await _queue
                .EnqueueAsync(
                    new DiagnosisJob
                    {
                        StudyId =
                            risk.StudyId
                    });

            return Ok(risk);
        }

        [HttpPut("study/{studyId}")]

        public async Task<IActionResult>
UpsertByStudy(

    int studyId,

    PatientRiskFactor updated)

        {
            // =================================
            // EXISTING
            // =================================

            var risk =
                await _context
                    .PatientRiskFactors

                    .FirstOrDefaultAsync(
                        x =>
                            x.StudyId
                            == studyId);

            // =================================
            // CREATE
            // =================================

            if (risk == null)
            {
                risk =
                    new PatientRiskFactor
                    {
                        StudyId =
                            studyId,

                        CreatedAt =
                            DateTime.Now
                    };

                _context
                    .PatientRiskFactors
                    .Add(risk);
            }

            // =================================
            // UPDATE
            // =================================

            risk.Weight =
                updated.Weight;

            risk.Height =
                updated.Height;

            risk.BMI =
                updated.BMI;

            risk.Smoking =
                updated.Smoking;

            risk.Diabetes =
                updated.Diabetes;

            risk.HypertensionTreatment =
                updated
                    .HypertensionTreatment;

            risk.ParentalHypertension =
                updated
                    .ParentalHypertension;

            // =================================
            // SAVE
            // =================================

            await _context
                .SaveChangesAsync();

            // =================================
            // RECALCULATE
            // =================================

            await _queue
                .EnqueueAsync(
                    new DiagnosisJob
                    {
                        StudyId =
                            studyId
                    });

            return Ok(risk);
        }


    }
}