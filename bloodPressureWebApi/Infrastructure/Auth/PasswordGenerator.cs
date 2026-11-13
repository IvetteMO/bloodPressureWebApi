using System.Security.Cryptography;

namespace bloodPressureWebApi.Infrastructure.Auth
{
    /// <summary>
    /// Contraseñas temporales: 14 caracteres aleatorios criptográficamente
    /// seguros, sin caracteres ambiguos (0/O, 1/l/I) para dictarlas sin error.
    /// </summary>
    public static class PasswordGenerator
    {
        private const string Alphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        public static string Generate(int length = 14) =>
            RandomNumberGenerator.GetString(Alphabet, length);
    }
}
