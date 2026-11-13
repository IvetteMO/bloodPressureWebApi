using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Auth;
using bloodPressureWebApi.Infrastructure.Auth;
using bloodPressureWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Services
{
    public enum LoginOutcome
    {
        Success,
        InvalidCredentials,
        LockedOut
    }

    public sealed record LoginResult(LoginOutcome Outcome, AppUser? User);

    /// <summary>
    /// Validación de credenciales y administración de usuarios.
    ///
    /// - Contraseñas con PasswordHasher (PBKDF2) de ASP.NET Core.
    /// - Bloqueo: 5 intentos fallidos -> 15 minutos.
    /// - El mensaje de error es el mismo si el correo no existe o si la
    ///   contraseña es incorrecta (no revela qué correos están registrados).
    /// </summary>
    public sealed class AuthService(
        ApplicationDbContext context,
        IPasswordHasher<AppUser> passwordHasher,
        ILogger<AuthService> logger)
    {
        public const int MaxFailedAttempts = 5;

        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        // Hash de referencia para gastar el mismo tiempo cuando el correo
        // no existe (evita distinguir correos por tiempo de respuesta).
        private static readonly AppUser DummyUser = new();
        private static readonly string DummyHash =
            new PasswordHasher<AppUser>().HashPassword(DummyUser, "dummy-password-for-timing");

        // =====================================================
        // LOGIN
        // =====================================================

        public async Task<LoginResult> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = NormalizeEmail(email);

            var user = await context.AppUsers
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

            if (user is null || !user.IsActive)
            {
                passwordHasher.VerifyHashedPassword(DummyUser, DummyHash, password);

                return new LoginResult(LoginOutcome.InvalidCredentials, null);
            }

            if (user.LockoutEndUtc > DateTime.UtcNow)
            {
                return new LoginResult(LoginOutcome.LockedOut, null);
            }

            var verification = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            if (verification == PasswordVerificationResult.Failed)
            {
                user.FailedLoginCount++;

                if (user.FailedLoginCount >= MaxFailedAttempts)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                    user.FailedLoginCount = 0;

                    logger.LogWarning(
                        "User {UserId} locked out after {Attempts} failed attempts.",
                        user.AppUserId,
                        MaxFailedAttempts);
                }

                await context.SaveChangesAsync(cancellationToken);

                return new LoginResult(
                    user.LockoutEndUtc > DateTime.UtcNow
                        ? LoginOutcome.LockedOut
                        : LoginOutcome.InvalidCredentials,
                    null);
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, password);
            }

            user.FailedLoginCount = 0;
            user.LockoutEndUtc = null;
            user.LastLoginAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new LoginResult(LoginOutcome.Success, user);
        }

        // =====================================================
        // CHANGE PASSWORD (el propio usuario)
        // =====================================================

        /// <summary>Regresa null si la contraseña actual no coincide.</summary>
        public async Task<AppUser?> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken)
        {
            var user = await context.AppUsers
                .FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive, cancellationToken);

            if (user is null)
            {
                return null;
            }

            var verification = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                currentPassword);

            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
            user.MustChangePassword = false;

            // Cierra las demás sesiones abiertas con la contraseña anterior.
            user.SecurityStamp = AppUser.NewSecurityStamp();

            await context.SaveChangesAsync(cancellationToken);

            return user;
        }

        // =====================================================
        // ADMIN: CREATE / UPDATE / RESET
        // =====================================================

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
            await context.AppUsers.AnyAsync(
                x => x.Email == NormalizeEmail(email),
                cancellationToken);

        public async Task<(AppUser User, string TemporaryPassword)> CreateUserAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            var temporaryPassword = PasswordGenerator.Generate();

            var user = new AppUser
            {
                Email = NormalizeEmail(request.Email!),
                DisplayName = request.DisplayName!.Trim(),
                Role = request.Role!,
                IsActive = true,
                MustChangePassword = true
            };

            user.PasswordHash = passwordHasher.HashPassword(user, temporaryPassword);

            context.AppUsers.Add(user);

            await context.SaveChangesAsync(cancellationToken);

            return (user, temporaryPassword);
        }

        public async Task<string> ResetPasswordAsync(
            AppUser user,
            CancellationToken cancellationToken)
        {
            var temporaryPassword = PasswordGenerator.Generate();

            user.PasswordHash = passwordHasher.HashPassword(user, temporaryPassword);
            user.MustChangePassword = true;
            user.FailedLoginCount = 0;
            user.LockoutEndUtc = null;
            user.SecurityStamp = AppUser.NewSecurityStamp();

            await context.SaveChangesAsync(cancellationToken);

            return temporaryPassword;
        }

        /// <summary>
        /// Cuántos administradores activos quedarían si este usuario dejara
        /// de serlo. Impide quedarse sin nadie que pueda administrar.
        /// </summary>
        public Task<int> CountOtherActiveAdminsAsync(int excludingUserId, CancellationToken cancellationToken) =>
            context.AppUsers.CountAsync(
                x => x.AppUserId != excludingUserId && x.IsActive && x.Role == AppRoles.Admin,
                cancellationToken);

        public static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();

        public static UserResponse ToResponse(AppUser user) =>
            new(
                user.AppUserId,
                user.Email,
                user.DisplayName,
                user.Role,
                user.IsActive,
                user.MustChangePassword,
                user.LockoutEndUtc > DateTime.UtcNow,
                user.CreatedAtUtc,
                user.LastLoginAtUtc);

        public static CurrentUserResponse ToCurrentUser(AppUser user) =>
            new(
                user.AppUserId,
                user.Email,
                user.DisplayName,
                user.Role,
                user.MustChangePassword);
    }
}
