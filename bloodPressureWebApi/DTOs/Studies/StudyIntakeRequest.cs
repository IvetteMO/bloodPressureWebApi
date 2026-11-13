using System.ComponentModel.DataAnnotations;

namespace bloodPressureWebApi.DTOs.Studies
{
    /// <summary>
    /// Lo necesario para dar de alta un estudio completo en una sola
    /// petición (multipart/form-data): horario, factores de riesgo y Excel.
    ///
    /// El IMC NO viene del cliente: lo calcula el backend con peso y talla.
    /// </summary>
    public class StudyIntakeRequest
    {
        // ---------- Horario ----------

        /// <summary>Hora de despertar, "HH:mm". Sin valor: 06:00.</summary>
        public TimeSpan? AwakeTime { get; init; }

        /// <summary>Hora de dormir, "HH:mm". Sin valor: 22:00.</summary>
        public TimeSpan? SleepTime { get; init; }

        // ---------- Antropometría ----------

        /// <summary>Peso en kg.</summary>
        [Required, Range(1, 500)]
        public double? Weight { get; init; }

        /// <summary>Talla en metros (ej. 1.65).</summary>
        [Required, Range(0.3, 2.5)]
        public double? Height { get; init; }

        // ---------- Factores de riesgo ----------

        [Required]
        public bool? Smoking { get; init; }

        [Required]
        public bool? Diabetes { get; init; }

        [Required]
        public bool? HypertensionTreatment { get; init; }

        [Required, Range(0, 2)]
        public byte? ParentalHypertension { get; init; }

        // ---------- Archivo MAPA ----------

        [Required]
        public IFormFile? File { get; init; }
    }

    /// <summary>
    /// Estudio nuevo para un paciente que ya existe.
    /// POST api/Studies/with-file
    /// </summary>
    public sealed class CreateStudyWithFileRequest : StudyIntakeRequest
    {
        [Required]
        public int? PatientId { get; init; }
    }
}
