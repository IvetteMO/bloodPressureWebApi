using System.ComponentModel.DataAnnotations;

namespace bloodPressureWebApi.DTOs.RiskFactors
{

    /// <summary>
    /// Campos que el médico puede editar desde el modal de factores de riesgo.
    ///
    /// Peso, talla e IMC NO forman parte de este contrato a propósito: se
    /// capturan al crear el estudio y alimentan FHS y CVDRisk. Si vinieran
    /// aquí como opcionales, un cliente que no los envía los borraría
    /// (que es justo el bug que corrige este cambio).
    ///
    /// Los campos son anulables + [Required] para que un JSON incompleto
    /// regrese 400 (ValidationProblemDetails) en lugar de guardar un valor
    /// por defecto en silencio.
    /// </summary>
    public sealed record UpdateRiskFactorsRequest(
        [Required] bool? Smoking,
        [Required] bool? Diabetes,
        [Required] bool? HypertensionTreatment,
        [Required, Range(0, 2)] byte? ParentalHypertension);
}
