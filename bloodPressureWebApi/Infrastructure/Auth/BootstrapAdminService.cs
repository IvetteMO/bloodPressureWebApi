using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Infrastructure.Auth
{
    /// <summary>
    /// Primer arranque: si no hay ningún usuario, crea un administrador con
    /// una contraseña temporal aleatoria y la escribe UNA vez en la consola.
    ///
    /// Así no hay contraseñas fijas en el código ni en appsettings.json.
    /// El correo se configura con Auth:BootstrapAdminEmail.
    /// Al entrar, la app obliga a cambiar la contraseña.
    /// </summary>
    public sealed class BootstrapAdminService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BootstrapAdminService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (await context.AppUsers.AnyAsync(cancellationToken))
                {
                    return;
                }

                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

                var email = (configuration["Auth:BootstrapAdminEmail"] ?? "admin@softdiagnosis.local")
                    .Trim()
                    .ToLowerInvariant();

                var temporaryPassword = PasswordGenerator.Generate();

                var admin = new AppUser
                {
                    Email = email,
                    DisplayName = "Administrator",
                    Role = AppRoles.Admin,
                    IsActive = true,
                    MustChangePassword = true
                };

                admin.PasswordHash = hasher.HashPassword(admin, temporaryPassword);

                context.AppUsers.Add(admin);

                await context.SaveChangesAsync(cancellationToken);

                logger.LogWarning(
                    "==========================================================\n" +
                    " FIRST RUN: administrator account created\n" +
                    "   Email:    {Email}\n" +
                    "   Password: {Password}\n" +
                    " This password is shown only once. You will be asked to\n" +
                    " change it after signing in.\n" +
                    "==========================================================",
                    email,
                    temporaryPassword);
            }
            catch (Exception ex)
            {
                // Lo más probable: falta aplicar la migración (Update-Database).
                logger.LogError(
                    ex,
                    "Could not create the bootstrap administrator. Did you run Update-Database for the AddAuthentication migration?");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
