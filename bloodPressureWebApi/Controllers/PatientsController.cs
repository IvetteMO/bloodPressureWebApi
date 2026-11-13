using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Patients;
using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using bloodPressureWebApi.DTOs.Studies;
using bloodPressureWebApi.Services;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly DiagnosisQueue _queue;

        public PatientsController(
            ApplicationDbContext context,
            DiagnosisQueue queue)
        {
            _context = context;
            _queue = queue;
        }

        // =====================================================
        // GET ALL
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetPatients()
        {
            var patients =
                await _context.Patients

                .AsNoTracking()

                .OrderByDescending(x =>
                    x.PatientId)

                .ToListAsync();

            return Ok(patients);
        }

        // =====================================================
        // GET SINGLE
        // =====================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatient(int id)
        {
            var patient =
                await _context.Patients

                .AsNoTracking()

                .FirstOrDefaultAsync(x =>
                    x.PatientId == id);

            if (patient == null)
            {
                return NotFound(new
                {
                    message =
                        "Patient not found"
                });
            }

            return Ok(patient);
        }

        // =====================================================
        // CREATE
        // =====================================================

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePatient(
            [FromBody] PatientRequest request,
            CancellationToken cancellationToken)
        {
            var newPatient = new Patients
            {
                CreatedAt = DateTime.Now
            };

            ApplyFields(newPatient, request);

            _context.ChangeTracker.Clear();

            await _context.Patients.AddAsync(newPatient, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // Misma forma que antes: el workflow del frontend lee patientId.
            return Ok(new
            {
                newPatient.PatientId,

                newPatient.FirstName,

                newPatient.LastName
            });
        }

        // =====================================================
        // CREATE WITH STUDY (multipart/form-data)
        //
        // Paciente + estudio + factores de riesgo + Excel en una sola
        // transaccion: si el Excel falla, no se crea nada.
        // =====================================================

        [HttpPost("with-study")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxStudyFileBytes)]
        [ProducesResponseType<StudyIntakeResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePatientWithStudy(
            [FromForm] RegisterPatientRequest request,
            [FromServices] StudyIntakeService intakeService,
            CancellationToken cancellationToken)
        {
            var result = await intakeService.RegisterPatientAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetPatient),
                new { id = result.PatientId },
                result);
        }

        // Un Excel de MAPA pesa unos KB; 10 MB es margen de sobra.
        private const long MaxStudyFileBytes = 10 * 1024 * 1024;

        // =====================================================
        // UPDATE
        //
        // Si cambia el sexo o la fecha de nacimiento, los diagnosticos
        // guardados ya no corresponden (FHS, CVDRisk y edad del corazon
        // dependen de ellos): se recalculan todos los estudios del paciente.
        // =====================================================

        [HttpPut("{id:int}")]
        [ProducesResponseType<PatientResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePatient(
            int id,
            [FromBody] PatientRequest request,
            CancellationToken cancellationToken)
        {
            var patient = await _context.Patients
                .Include(x => x.Studies)
                .FirstOrDefaultAsync(
                    x => x.PatientId == id,
                    cancellationToken);

            if (patient is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Paciente no encontrado",
                    detail: $"No existe el paciente {id}.");
            }

            var affectsDiagnosis =
                patient.Sex != request.Sex
                || patient.BirthDate?.Date != request.BirthDate?.Date;

            ApplyFields(patient, request);

            var studiesToRecalculate = affectsDiagnosis
                ? patient.Studies.ToList()
                : new List<MAPAStudy>();

            foreach (var study in studiesToRecalculate)
            {
                study.ProcessingStatus = StudyProcessingStatus.Queued;
                study.ProcessingError = null;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Sin token a proposito: los cambios ya se guardaron.
            foreach (var study in studiesToRecalculate)
            {
                await _queue.EnqueueAsync(
                    new DiagnosisJob
                    {
                        StudyId = study.StudyId
                    });
            }

            return Ok(PatientResponse.From(patient, studiesToRecalculate.Count));
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static void ApplyFields(
            Patients patient,
            PatientRequest request)
        {
            patient.FirstName = request.FirstName?.Trim();
            patient.LastName = request.LastName?.Trim();
            patient.BirthDate = request.BirthDate?.Date;
            patient.Sex = request.Sex;
            patient.PhoneNumber = NullIfBlank(request.PhoneNumber);
            patient.Email = NullIfBlank(request.Email);
            patient.Notes = NullIfBlank(request.Notes);
        }

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient =
                await _context
                    .Patients

                    .FirstOrDefaultAsync(x =>
                        x.PatientId == id);

            if (patient == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Patient not found"
                });
            }

            _context
                .Patients
                .Remove(patient);

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }
    }
}

