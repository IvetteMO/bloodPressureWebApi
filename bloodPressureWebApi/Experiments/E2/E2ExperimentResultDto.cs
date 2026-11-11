namespace bloodPressureWebApi.Experiments.E2
{
    public class E2ExperimentResultDto
    {
        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public int StudiesSelected { get; set; }

        // NUEVO: inicializacion del MATLAB Runtime (constructor de MatlabService)
        public double? MatlabFirstInitializationMs { get; set; }

        public int MatlabInstancesCreated { get; set; }


        public int StudiesProcessed { get; set; }

        public string OutputFolder { get; set; } = string.Empty;

        public List<string> Errors { get; set; } = new();

        public double TotalExecutionTimeSeconds { get; set; }

        public double ThroughputStudiesPerSecond { get; set; }

        public List<DiagnosisBenchmarkResult> Results { get; set; }
            = new();
    }
}