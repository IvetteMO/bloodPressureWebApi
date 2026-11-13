using bloodPressureWebApi.DTOs.Studies;
using System.ComponentModel.DataAnnotations;

namespace bloodPressureWebApi.DTOs.Patients
{
        /// <summary>
        /// Alta de paciente con su primer estudio, en una sola petición
        /// (multipart/form-data). Mismas reglas que PatientRequest.
        /// POST api/Patients/with-study
        /// </summary>
        public sealed class RegisterPatientRequest : StudyIntakeRequest
        {
            [Required, StringLength(100)]
            public string? FirstName { get; init; }

            [Required, StringLength(100)]
            public string? LastName { get; init; }

            public DateTime? BirthDate { get; init; }

            [Required, Range(0, 1)]
            public byte? Sex { get; init; }

            [StringLength(30)]
            public string? PhoneNumber { get; init; }

            [EmailAddress, StringLength(256)]
            public string? Email { get; init; }

            public string? Notes { get; init; }
        }
    }

