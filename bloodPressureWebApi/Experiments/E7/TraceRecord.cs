namespace bloodPressureWebApi.Experiments.E7
{
    public class TraceRecord
    {
        public Guid ExperimentId { get; set; }

        public int Iteration { get; set; }

        public int StudyId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public bool Success { get; set; }

        // Tiempos (ms): features incluye la red neuronal modular
        public double FeaturesMs { get; set; }

        public double DiagnosisMs { get; set; }

        public double TotalMs { get; set; }

        // Firma de valores calculados y su hash
        public string Signature { get; set; } = "";

        public string ResultHash { get; set; } = "";

        // Procedencia real (medida en ejecucion, no constantes del codigo)
        public string ModelAssemblySha256 { get; set; } = "";

        public string ModelAssemblyVersion { get; set; } = "";

        public string MWArrayAssemblyVersion { get; set; } = "";

        public string ErrorMessage { get; set; } = "";
    }
}
