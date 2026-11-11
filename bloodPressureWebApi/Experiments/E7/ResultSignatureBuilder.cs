using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using bloodPressureWebApi.Models;

namespace bloodPressureWebApi.Experiments.E7
{
    /// <summary>
    /// Firma determinista de los RESULTADOS CALCULADOS de un estudio.
    ///
    /// CAMBIO: la version anterior incluia ModelVersion, FeaturePipelineVersion,
    /// MatlabRuntimeVersion (constantes escritas en el codigo) y ConfidenceScore
    /// (0.95 fijo). Una constante siempre "coincide", asi que inflaba la
    /// reproducibilidad sin aportar evidencia. Ahora la firma contiene solo
    /// valores calculados por el pipeline: salidas de la ANN, medias dia/noche,
    /// ratios, salidas de los FIS, etiquetas y riesgos de Framingham.
    /// La procedencia del software se registra aparte (ver E7 Provenance).
    /// </summary>
    public static class ResultSignatureBuilder
    {
        public static string BuildOutputSignature(
            MAPAFeature? feature,
            AIDiagnosis diagnosis)
        {
            return string.Join("|",
                diagnosis.StudyId,

                // Red neuronal modular
                N(feature?.SystolicTrend),
                N(feature?.DiastolicTrend),
                N(feature?.PulseTrend),

                // Rama nocturna
                N(feature?.DaySystolicMean),
                N(feature?.DayDiastolicMean),
                N(feature?.NightSystolicMean),
                N(feature?.NightDiastolicMean),
                N(feature?.SystolicNightRatio),
                N(feature?.DiastolicNightRatio),

                // Sistemas difusos
                N(diagnosis.FuzzyHeartRC),
                N(diagnosis.FuzzyBPRisk),
                N(diagnosis.FuzzyLoadC),
                N(diagnosis.FuzzyNocturnalC),

                // Decisiones
                diagnosis.FinalRiskLevel ?? "",
                diagnosis.NocturnalProfile ?? "",
                diagnosis.LoadDiagnosis ?? "",
                diagnosis.HeartRateDiagnosis ?? "");
                // Framingham excluido: factores de riesgo no disponibles,
                // fuera del modelo validado (E1-B).
        }

        /// <summary>Firma solo del diagnostico (cuando no se recalcula features).</summary>
        public static string BuildOutputSignature(AIDiagnosis diagnosis)
            => BuildOutputSignature(null, diagnosis);

        public static string ComputeHash(string signature)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(signature));
            return Convert.ToHexString(hash);
        }

        public static string ComputeFileHash(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        // G17 = representacion exacta (ida y vuelta) de un double
        private static string N(double? value)
            => value.HasValue
                ? value.Value.ToString("G17", CultureInfo.InvariantCulture)
                : "null";
    }
}
