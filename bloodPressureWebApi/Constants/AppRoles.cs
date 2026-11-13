namespace bloodPressureWebApi.Constants
{
    /// <summary>
    /// Roles de la aplicación.
    ///
    /// - Admin: además de todo lo clínico, administra usuarios.
    /// - Medico / Investigador: mismos permisos clínicos; el rol sirve
    ///   para la auditoría (quién hizo qué).
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "ADMIN";

        public const string Medico = "MEDICO";

        public const string Investigador = "INVESTIGADOR";

        public static readonly IReadOnlyList<string> All =
            new[] { Admin, Medico, Investigador };

        public static bool IsValid(string? role) =>
            role is not null && All.Contains(role);
    }
}
