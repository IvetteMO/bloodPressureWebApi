using System.ComponentModel.DataAnnotations;

namespace bloodPressureWebApi.DTOs.Auth
{
    public sealed record LoginRequest(
        [Required, EmailAddress, StringLength(256)] string? Email,
        [Required, StringLength(200)] string? Password);

    public sealed record ChangePasswordRequest(
        [Required, StringLength(200)] string? CurrentPassword,
        [Required, StringLength(200, MinimumLength = PasswordRules.MinLength)] string? NewPassword);

    /// <summary>Usuario de la sesión actual (GET api/auth/me).</summary>
    public sealed record CurrentUserResponse(
        int UserId,
        string Email,
        string DisplayName,
        string Role,
        bool MustChangePassword);

    // ---------- Administración de usuarios (rol ADMIN) ----------

    public sealed record UserResponse(
        int UserId,
        string Email,
        string DisplayName,
        string Role,
        bool IsActive,
        bool MustChangePassword,
        bool IsLockedOut,
        DateTime CreatedAtUtc,
        DateTime? LastLoginAtUtc);

    public sealed record CreateUserRequest(
        [Required, EmailAddress, StringLength(256)] string? Email,
        [Required, StringLength(150)] string? DisplayName,
        [Required] string? Role);

    public sealed record UpdateUserRequest(
        [Required, StringLength(150)] string? DisplayName,
        [Required] string? Role,
        [Required] bool? IsActive);

    /// <summary>
    /// La contraseña temporal se muestra UNA sola vez, al crear el usuario
    /// o al restablecerla. El usuario debe cambiarla al entrar.
    /// </summary>
    public sealed record TemporaryPasswordResponse(
        UserResponse User,
        string TemporaryPassword);

    public static class PasswordRules
    {
        public const int MinLength = 10;
    }
}
