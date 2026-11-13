using bloodPressureWebApi.Models;

namespace bloodPressureWebApi.DTOs.RiskFactors
{
        
    /// <summary>
    /// Respuesta de la edición de factores de riesgo.
    ///
    /// Mantiene la misma forma (camelCase, valores 0/1) que el arreglo
    /// riskFactors de GET /api/Studies/full/{studyId}, para que el frontend
    /// pueda usar ambos sin conversiones.
    ///
    /// ProcessingStatus indica que el diagnóstico se está recalculando en
    /// segundo plano: hasta que llegue a COMPLETED, el diagnóstico que se
    /// muestre corresponde a los factores anteriores.
    /// </summary>
    public sealed record RiskFactorsResponse(
        int RiskFactorId,
        int StudyId,
        double? Weight,
        double? Height,
        double? Bmi,
        byte? Smoking,
        byte? Diabetes,
        byte? HypertensionTreatment,
        byte? ParentalHypertension,
        string? ProcessingStatus)
        {
            public static RiskFactorsResponse From(
                PatientRiskFactor risk,
                string? processingStatus) =>
                new(
                    risk.RiskFactorId,
                    risk.StudyId,
                    risk.Weight,
                    risk.Height,
                    risk.BMI,
                    risk.Smoking,
                    risk.Diabetes,
                    risk.HypertensionTreatment,
                    risk.ParentalHypertension,
                    processingStatus);
        
    }
}
