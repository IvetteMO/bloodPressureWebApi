using bloodPressureWebApi.Constants;

namespace bloodPressureWebApi.Models
{
    /// <summary>
    /// Usuario de la aplicación (médico, investigador o administrador).
    ///
    /// La contraseña se guarda con PasswordHasher de ASP.NET Core
    /// (PBKDF2 con sal), nunca en texto plano.
    ///
    /// SecurityStamp cambia cuando se desactiva al usuario, se le cambia
    /// el rol o la contraseña: las sesiones abiertas con el sello anterior
    /// dejan de ser válidas en la siguiente petición.
    /// </summary>
    public class AppUser
    {
        public int AppUserId { get; set; }

        public string Email { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public string Role { get; set; } = AppRoles.Medico;

        public bool IsActive { get; set; } = true;

        /// <summary>true con contraseñas temporales (alta o restablecimiento).</summary>
        public bool MustChangePassword { get; set; }

        public int FailedLoginCount { get; set; }

        public DateTime? LockoutEndUtc { get; set; }

        public string SecurityStamp { get; set; } = NewSecurityStamp();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAtUtc { get; set; }

        public static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
    }
}
