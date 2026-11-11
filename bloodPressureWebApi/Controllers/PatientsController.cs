using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Shared;
using bloodPressureWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(
            ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult>CreatePatient([FromBody]PatientDto dto)
        {
            var newPatient =
                new Patients
                {
                    FirstName =
                        dto.FirstName,

                    LastName =
                        dto.LastName,

                    BirthDate =
                        dto.BirthDate,

                    Sex =
                        dto.Sex,

                    PhoneNumber =
                        dto.PhoneNumber,

                    Email =
                        dto.Email,

                    Notes =
                        dto.Notes,

                    CreatedAt =
                        DateTime.Now
                };

            _context.ChangeTracker.Clear();

            await _context
                .Patients
                .AddAsync(newPatient);

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                newPatient.PatientId,

                newPatient.FirstName,

                newPatient.LastName
            });
        }
        // =====================================================
        // UPDATE
        // =====================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id,Patients updated)
        {
            var patient =
                await _context.Patients
                .FirstOrDefaultAsync(
                    x => x.PatientId == id
                );

            if (patient == null)
            {
                return NotFound(new
                {
                    message =
                        "Patient not found"
                });
            }

            patient.FirstName =
                updated.FirstName;

            patient.LastName =
                updated.LastName;

            patient.BirthDate =
                updated.BirthDate;

            patient.Sex =
                updated.Sex;

            patient.PhoneNumber =
                updated.PhoneNumber;

            patient.Email =
                updated.Email;

            patient.Notes =
                updated.Notes;

            await _context
                .SaveChangesAsync();

            return Ok(patient);
        }

        [HttpDelete("{id}")]public async Task<IActionResult>DeletePatient(int id)
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