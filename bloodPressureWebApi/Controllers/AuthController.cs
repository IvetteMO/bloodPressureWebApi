using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Auth;
using bloodPressureWebApi.Infrastructure.Auth;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    /// <summary>
    /// Inicio y cierre de sesión con cookie HttpOnly.
    ///
    /// La cookie no es legible desde JavaScript (protege contra robo de
    /// sesión por XSS). El frontend solo llama a estos endpoints y deja que
    /// el navegador envíe la cookie en cada petición.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController(
        AuthService authService,
        ApplicationDbContext context,
        ILogger<AuthController> logger) : ControllerBase
    {
        // =====================================================
        // LOGIN
        // =====================================================

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthDefaults.LoginRateLimitPolicy)]
        [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            var result = await authService.ValidateCredentialsAsync(
                request.Email!,
                request.Password!,
                cancellationToken);

            await AuditAsync(
                result.Outcome == LoginOutcome.Success ? "LOGIN" : "LOGIN_FAILED",
                result.User,
                request.Email,
                cancellationToken);

            if (result.Outcome == LoginOutcome.LockedOut)
            {
                return Problem(
                    statusCode: StatusCodes.Status423Locked,
                    title: "Cuenta bloqueada temporalmente",
                    detail: $"Demasiados intentos fallidos. Intenta de nuevo en {AuthService.LockoutDuration.TotalMinutes:F0} minutos.");
            }

            if (result.Outcome != LoginOutcome.Success)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "No se pudo iniciar sesión",
                    detail: "Correo o contraseña incorrectos.");
            }

            await HttpContext.SignInAsync(
                AuthDefaults.Scheme,
                AuthClaims.CreatePrincipal(result.User!));

            return Ok(AuthService.ToCurrentUser(result.User!));
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await AuditAsync("LOGOUT", null, User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value, cancellationToken);
            }

            await HttpContext.SignOutAsync(AuthDefaults.Scheme);

            return NoContent();
        }

        // =====================================================
        // ME
        // =====================================================

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var user = await context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId, cancellationToken);

            // La cookie se valida en cada petición (ver Program.cs), así que
            // aquí el usuario siempre existe y está activo.
            return user is null
                ? Unauthorized()
                : Ok(AuthService.ToCurrentUser(user));
        }

        // =====================================================
        // CHANGE PASSWORD
        // =====================================================

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (request.NewPassword == request.CurrentPassword)
            {
                ModelState.AddModelError(
                    nameof(request.NewPassword),
                    "La nueva contraseña debe ser distinta de la actual.");

                return ValidationProblem(ModelState);
            }

            var user = await authService.ChangePasswordAsync(
                User.GetUserId()!.Value,
                request.CurrentPassword!,
                request.NewPassword!,
                cancellationToken);

            if (user is null)
            {
                ModelState.AddModelError(
                    nameof(request.CurrentPassword),
                    "La contraseña actual no es correcta.");

                return ValidationProblem(ModelState);
            }

            // El sello de seguridad cambió: se renueva la cookie de ESTA
            // sesión; las demás quedan cerradas.
            await HttpContext.SignInAsync(
                AuthDefaults.Scheme,
                AuthClaims.CreatePrincipal(user));

            await AuditAsync("PASSWORD_CHANGED", user, user.Email, cancellationToken);

            return Ok(AuthService.ToCurrentUser(user));
        }

        // =====================================================
        // AUDIT
        // =====================================================

        private async Task AuditAsync(
            string action,
            AppUser? user,
            string? email,
            CancellationToken cancellationToken)
        {
            try
            {
                context.AuditLogs.Add(new AuditLog
                {
                    Action = action,
                    EntityName = "Auth",
                    EntityId = user?.AppUserId ?? 0,
                    UserId = user?.AppUserId ?? User.GetUserId(),
                    UserEmail = email is null ? null : AuthService.NormalizeEmail(email),
                    UserRole = user?.Role,
                    HttpMethod = Request.Method,
                    Path = Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // La auditoría no debe impedir iniciar o cerrar sesión.
                logger.LogError(ex, "Could not write audit entry {Action}.", action);
            }
        }
    }
}
