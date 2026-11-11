using bloodPressureWebApi.Experiments.E2;

namespace bloodPressureWebApi.Experiments.E3
{
    public class E3ExperimentResultDto
    {
        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public int StudiesSelected { get; set; }

        // NUEVO: inicializacion del MATLAB Runtime (constructor de MatlabService)
        public double? MatlabFirstInitializationMs { get; set; }

        public int MatlabInstancesCreated { get; set; }


        public int StudiesProcessed { get; set; }

        public List<int> ConcurrencyLevels { get; set; } = new();

        // Compatibilidad con la version anterior (primer nivel evaluado)
        public int Concurrency { get; set; }

        public double WarmUpSeconds { get; set; }

        public string OutputFolder { get; set; } = string.Empty;

        public List<string> Errors { get; set; } = new();

        public List<E3IterationSummary> Iterations { get; set; } = new();

        public double TotalExecutionTimeSeconds { get; set; }

        // Ya no se devuelven las 1,500+ filas individuales en la respuesta
        // (estan en DiagnosisResults.csv); solo el resumen por iteracion.
    }

    public class E3IterationSummary
    {
        public int ConcurrencyLevel { get; set; }

        public int Iteration { get; set; }

        public int Studies { get; set; }

        public int Errors { get; set; }

        /// <summary>Tiempo de pared desde que inicia hasta que termina el lote.</summary>
        public double MakespanMs { get; set; }

        public double ThroughputStudiesPerSecond { get; set; }

        public DateTime StartedAtUtc { get; set; }
    }
}
