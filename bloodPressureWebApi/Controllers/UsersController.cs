using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Auth;
using bloodPressureWebApi.Infrastructure.Auth;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    /// <summary>
    /// Administración de usuarios. Solo rol ADMIN.
    ///
    /// No hay auto-registro: el administrador crea la cuenta, la app genera
    /// una contraseña temporal (se muestra una sola vez) y el usuario la
    /// cambia al entrar por primera vez.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = AppRoles.Admin)]
    public sealed class UsersController(
        ApplicationDbContext context,
        AuthService authService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            var users = await context.AppUsers
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .ToListAsync(cancellationToken);

            return Ok(users.Select(AuthService.ToResponse).ToList());
        }

        [HttpPost]
        [ProducesResponseType<TemporaryPasswordResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            if (!AppRoles.IsValid(request.Role))
            {
                return InvalidRole();
            }

            if (await authService.EmailExistsAsync(request.Email!, cancellationToken))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Correo ya registrado",
                    detail: $"Ya existe un usuario con el correo {request.Email}.");
            }

            var (user, temporaryPassword) =
                await authService.CreateUserAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetUsers),
                null,
                new TemporaryPasswordResponse(AuthService.ToResponse(user), temporaryPassword));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateUser(
            int id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            if (!AppRoles.IsValid(request.Role))
            {
                return InvalidRole();
            }

            var user = await context.AppUsers
                .FirstOrDefaultAsync(x => x.AppUserId == id, cancellationToken);

            if (user is null)
            {
                return UserNotFound(id);
            }

            var losesAdmin =
                user.Role == AppRoles.Admin
                && user.IsActive
                && (request.Role != AppRoles.Admin || request.IsActive == false);

            if (losesAdmin
                && await authService.CountOtherActiveAdminsAsync(user.AppUserId, cancellationToken) == 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Se necesita al menos un administrador",
                    detail: "Este es el único administrador activo. Asigna el rol a otro usuario antes de cambiarlo o desactivarlo.");
            }

            var sessionMustEnd =
                user.Role != request.Role || user.IsActive != request.IsActive;

            user.DisplayName = request.DisplayName!.Trim();
            user.Role = request.Role!;
            user.IsActive = request.IsActive!.Value;

            // Cambió el rol o se desactivó: sus sesiones abiertas terminan
            // en la siguiente petición.
            if (sessionMustEnd)
            {
                user.SecurityStamp = AppUser.NewSecurityStamp();
            }

            await context.SaveChangesAsync(cancellationToken);

            return Ok(AuthService.ToResponse(user));
        }

        [HttpPost("{id:int}/reset-password")]
        [ProducesResponseType<TemporaryPasswordResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await context.AppUsers
                .FirstOrDefaultAsync(x => x.AppUserId == id, cancellationToken);

            if (user is null)
            {
                return UserNotFound(id);
            }

            var temporaryPassword =
                await authService.ResetPasswordAsync(user, cancellationToken);

            return Ok(new TemporaryPasswordResponse(
                AuthService.ToResponse(user),
                temporaryPassword));
        }

        private IActionResult InvalidRole()
        {
            ModelState.AddModelError(
                "Role",
                $"Rol inválido. Valores permitidos: {string.Join(", ", AppRoles.All)}.");

            return ValidationProblem(ModelState);
        }

        private IActionResult UserNotFound(int id) =>
            Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Usuario no encontrado",
                detail: $"No existe el usuario {id}.");
    }
}
