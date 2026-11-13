using System.ComponentModel.DataAnnotations;

namespace bloodPressureWebApi.DTOs.Patients
{
        /// <summary>
        /// Datos del paciente para crear (POST) y editar (PUT).
        ///
        /// Los nombres coinciden con la entidad Patients para que el frontend
        /// use un solo contrato: phoneNumber (no phone), notes (no clinicalNotes)
        /// y sex como 0/1 (no "Male"/"Female").
        ///
        /// Sexo y fecha de nacimiento alimentan FHS, CVDRisk y la edad del
        /// corazón, por eso el sexo es obligatorio: si faltara, el diagnóstico
        /// asumiría hombre (Sex ?? 0) sin avisar.
        /// </summary>
        public sealed record PatientRequest(
            [Required, StringLength(100)] string? FirstName,
            [Required, StringLength(100)] string? LastName,
            DateTime? BirthDate,
            [Required, Range(0, 1)] byte? Sex,
            [StringLength(30)] string? PhoneNumber,
            [EmailAddress, StringLength(256)] string? Email,
            string? Notes);
    }

