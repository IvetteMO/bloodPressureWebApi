using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace bloodPressureWebApi.Infrastructure.Experiments
{
    /// <summary>
    /// Los controladores de experimentos no piden sesión: sus scripts los
    /// llaman sin iniciar sesión, igual que cuando se escribió el artículo.
    ///
    /// Solo existen con Experiments:Enabled = true (Development), así que
    /// en producción esto no abre nada.
    /// </summary>
    public sealed class ExperimentAnonymousConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (!controller.ControllerType.IsDefined(typeof(ExperimentEndpointAttribute), inherit: true))
            {
                return;
            }

            // Se agrega en cada acción: es la metadata que lee el
            // middleware de autorización al resolver el endpoint.
            foreach (var selector in controller.Actions.SelectMany(action => action.Selectors))
            {
                selector.EndpointMetadata.Add(new AllowAnonymousAttribute());
            }
        }
    }
}
