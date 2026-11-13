namespace bloodPressureWebApi.Infrastructure.Experiments
{
    /// <summary>
    /// Marca un controlador como herramienta de experimentación o de
    /// desarrollo. Solo se publica cuando Experiments:Enabled = true
    /// (appsettings.Development.json); en producción la ruta no existe.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class ExperimentEndpointAttribute : Attribute
    {
    }
}
