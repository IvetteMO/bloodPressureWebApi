using System.Security.Claims;
using bloodPressureWebApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Infrastructure.Auth
{
    /// <summary>
    /// Se ejecuta en cada petición con cookie: si el usuario fue
    /// desactivado, cambió de rol o de contraseña (SecurityStamp distinto),
    /// la sesión se rechaza y se borra la cookie.
    ///
    /// Es una consulta por llave primaria a una tabla pequeña; a cambio,
    /// desactivar a alguien tiene efecto inmediato.
    /// </summary>
    public static class SessionValidator
    {
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;

            var userId = principal?.GetUserId();

            var stamp = principal?.FindFirstValue(AuthClaims.SecurityStamp);

            if (userId is null || stamp is null)
            {
                await RejectAsync(context);
                return;
            }

            var db = context.HttpContext.RequestServices
                .GetRequiredService<ApplicationDbContext>();

            var isValid = await db.AppUsers
                .AsNoTracking()
                .AnyAsync(x =>
                    x.AppUserId == userId
                    && x.IsActive
                    && x.SecurityStamp == stamp,
                    context.HttpContext.RequestAborted);

            if (!isValid)
            {
                await RejectAsync(context);
            }
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();

            await context.HttpContext.SignOutAsync(AuthDefaults.Scheme);
        }
    }
}
