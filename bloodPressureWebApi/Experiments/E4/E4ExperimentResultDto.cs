namespace bloodPressureWebApi.Experiments.E4
{
    public class E4ExperimentResultDto
    {
        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public int StudiesProcessed { get; set; }

        public int Iterations { get; set; }

        public string OutputFolder { get; set; } = string.Empty;

        public double WarmUpSeconds { get; set; }

        public int TotalJobs { get; set; }

        public int TotalFailures { get; set; }

        /// <summary>Resultados identicos a la linea base (debe = TotalJobs).</summary>
        public int ResultsMatchingBaseline { get; set; }

        /// <summary>Un registro por configuracion e iteracion (makespan, throughput, latencias).</summary>
        public List<BatchRecord> Batches { get; set; } = new();

        public double TotalExecutionTimeSeconds { get; set; }
    }
}
