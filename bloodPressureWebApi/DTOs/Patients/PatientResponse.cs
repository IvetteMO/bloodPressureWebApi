namespace bloodPressureWebApi.DTOs.Patients
{
    /// <summary>
    /// Paciente sin colecciones de navegación.
    ///
    /// StudiesQueued indica cuántos estudios se mandaron a recalcular
    /// porque cambió el sexo o la fecha de nacimiento.
    /// </summary>
    public sealed record PatientResponse(
        int PatientId,
        string? ExternalPatientCode,
        string? FirstName,
        string? LastName,
        DateTime? BirthDate,
        byte? Sex,
        string? PhoneNumber,
        string? Email,
        string? Notes,
        DateTime CreatedAt,
        int StudiesQueued)
    {
        public static PatientResponse From(Models.Patients patient, int studiesQueued = 0) =>
            new(
                patient.PatientId,
                patient.ExternalPatientCode,
                patient.FirstName,
                patient.LastName,
                patient.BirthDate,
                patient.Sex,
                patient.PhoneNumber,
                patient.Email,
                patient.Notes,
                patient.CreatedAt,
                studiesQueued);
    }
}
