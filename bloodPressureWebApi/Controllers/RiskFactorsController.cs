using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.RiskFactors;
using bloodPressureWebApi.DTOs.Studies;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet("study/{studyId:int}")]
        [ProducesResponseType<RiskFactorsResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByStudy(
            int studyId,
            CancellationToken cancellationToken)
        {
            // Mismo criterio que AIDiagnosisService y UpsertByStudy:
            // el registro mas reciente del estudio.
            var risk = await _context.PatientRiskFactors
                .AsNoTracking()
                .Where(x => x.StudyId == studyId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.RiskFactorId)
                .Select(x => new
                {
                    Risk = x,
                    x.Study!.ProcessingStatus
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (risk is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Factores de riesgo no encontrados",
                    detail: $"El estudio {studyId} no tiene factores de riesgo registrados.");
            }

            return Ok(RiskFactorsResponse.From(risk.Risk, risk.ProcessingStatus));
        }

        // =====================================
        // UPDATE BY STUDY (endpoint que usa el frontend)
        //
        // PUT api/RiskFactors/study/{studyId}
        //
        // Actualiza SOLO los campos editables del modal. Peso, talla e IMC
        // se conservan. Si el estudio aun no tiene factores, se crean.
        // El diagnostico se recalcula en segundo plano: responde 202.
        // =====================================

        [HttpPut("study/{studyId:int}")]
        [ProducesResponseType<RiskFactorsResponse>(StatusCodes.Status202Accepted)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpsertByStudy(
            int studyId,
            [FromBody] UpdateRiskFactorsRequest request,
            CancellationToken cancellationToken)
        {
            var study = await _context.MAPAStudies
                .Include(x => x.RiskFactors)
                .FirstOrDefaultAsync(
                    x => x.StudyId == studyId,
                    cancellationToken);

            if (study is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Estudio no encontrado",
                    detail: $"No existe el estudio {studyId}.");
            }

            // Mismo criterio que AIDiagnosisService: se usa el registro
            // mas reciente. Asi lo que se edita es lo que se diagnostica.
            var risk = study.RiskFactors
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.RiskFactorId)
                .FirstOrDefault();

            if (risk is null)
            {
                risk = new PatientRiskFactor
                {
                    StudyId = studyId,
                    CreatedAt = DateTime.Now
                };

                _context.PatientRiskFactors.Add(risk);
            }

            ApplyEditableFields(risk, request);

            return await SaveAndRecalculateAsync(
                study,
                risk,
                cancellationToken);
        }

        // =====================================
        // UPDATE BY RISK FACTOR ID
        //
        // PUT api/RiskFactors/{riskFactorId}
        //
        // Se conserva por compatibilidad (Swagger / clientes externos).
        // OJO: el parametro es el RiskFactorId, NO el StudyId.
        // =====================================

        [HttpPut("{riskFactorId:int}")]
        [ProducesResponseType<RiskFactorsResponse>(StatusCodes.Status202Accepted)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            int riskFactorId,
            [FromBody] UpdateRiskFactorsRequest request,
            CancellationToken cancellationToken)
        {
            var risk = await _context.PatientRiskFactors
                .Include(x => x.Study)
                .FirstOrDefaultAsync(
                    x => x.RiskFactorId == riskFactorId,
                    cancellationToken);

            if (risk?.Study is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Factores de riesgo no encontrados",
                    detail: $"No existe el registro de factores de riesgo {riskFactorId}.");
            }

            ApplyEditableFields(risk, request);

            return await SaveAndRecalculateAsync(
                risk.Study,
                risk,
                cancellationToken);
        }

        // =====================================
        // HELPERS
        // =====================================

        private static void ApplyEditableFields(
            PatientRiskFactor risk,
            UpdateRiskFactorsRequest request)
        {
            // [Required] garantiza que no son null cuando llega aqui
            // ([ApiController] responde 400 antes de entrar al metodo).
            risk.Smoking = ToFlag(request.Smoking!.Value);
            risk.Diabetes = ToFlag(request.Diabetes!.Value);
            risk.HypertensionTreatment = ToFlag(request.HypertensionTreatment!.Value);
            risk.ParentalHypertension = request.ParentalHypertension!.Value;
        }

        private static byte ToFlag(bool value) => value ? (byte)1 : (byte)0;

        private async Task<IActionResult> SaveAndRecalculateAsync(
            MAPAStudy study,
            PatientRiskFactor risk,
            CancellationToken cancellationToken)
        {
            // El diagnostico guardado ya no corresponde a los factores
            // nuevos: se marca en cola para que el frontend lo sepa.
            study.ProcessingStatus = StudyProcessingStatus.Queued;
            study.ProcessingError = null;

            await _context.SaveChangesAsync(cancellationToken);

            // Sin token a proposito: los cambios ya se guardaron, el
            // recalculo debe encolarse aunque el cliente cancele.
            await _queue.EnqueueAsync(
                new DiagnosisJob
                {
                    StudyId = study.StudyId
                });

            return AcceptedAtAction(
                nameof(GetByStudy),
                new { studyId = study.StudyId },
                RiskFactorsResponse.From(risk, study.ProcessingStatus));
        }
    }
}