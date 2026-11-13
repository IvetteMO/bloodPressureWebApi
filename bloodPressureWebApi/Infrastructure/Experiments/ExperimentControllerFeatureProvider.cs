using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace bloodPressureWebApi.Infrastructure.Experiments
{
    /// <summary>
    /// Quita del enrutamiento los controladores marcados con
    /// [ExperimentEndpoint] cuando los experimentos están deshabilitados.
    ///
    /// Se ejecuta después del proveedor por defecto de MVC, así que solo
    /// filtra la lista que este ya armó.
    /// </summary>
    public sealed class ExperimentControllerFeatureProvider(bool experimentsEnabled)
        : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(
            IEnumerable<ApplicationPart> parts,
            ControllerFeature feature)
        {
            if (experimentsEnabled)
            {
                return;
            }

            var experimentControllers = feature.Controllers
                .Where(type => type.IsDefined(typeof(ExperimentEndpointAttribute), inherit: true))
                .ToList();

            foreach (var controller in experimentControllers)
            {
                feature.Controllers.Remove(controller);
            }
        }
    }
}
