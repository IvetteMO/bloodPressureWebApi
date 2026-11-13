using System.Security.Claims;
using bloodPressureWebApi.Models;

namespace bloodPressureWebApi.Infrastructure.Auth
{
    /// <summary>
    /// Claims que viajan en la cookie de sesión y cómo leerlos.
    /// </summary>
    public static class AuthClaims
    {
        public const string SecurityStamp = "sd:stamp";

        public static ClaimsPrincipal CreatePrincipal(AppUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.AppUserId.ToString()),
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new(SecurityStamp, user.SecurityStamp)
            };

            var identity = new ClaimsIdentity(
                claims,
                AuthDefaults.Scheme);

            return new ClaimsPrincipal(identity);
        }

        public static int? GetUserId(this ClaimsPrincipal principal) =>
            int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : null;
    }

    public static class AuthDefaults
    {
        public const string Scheme = "SoftDiagnosisCookie";

        public const string CookieName = "sd.auth";

        public const string LoginRateLimitPolicy = "login";
    }
}
