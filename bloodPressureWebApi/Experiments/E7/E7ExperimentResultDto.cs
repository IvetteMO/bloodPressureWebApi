namespace bloodPressureWebApi.Experiments.E7
{
    public class E7ExperimentResultDto
    {
        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public int StudiesProcessed { get; set; }

        public int Iterations { get; set; }

        public string OutputFolder { get; set; } = string.Empty;

        // ============================================
        // REPRODUCIBILIDAD
        // ============================================

        public int SuccessfulExecutions { get; set; }

        public int FailedExecutions { get; set; }

        /// <summary>Estudios con todas sus ejecuciones exitosas y un solo hash.</summary>
        public int StudiesFullyReproducible { get; set; }

        /// <summary>Hashes distintos en total (debe ser = numero de estudios).</summary>
        public int DistinctHashesOverall { get; set; }

        public E7Provenance Provenance { get; set; } = new();

        public double TotalExecutionTimeSeconds { get; set; }

        // Las trazas completas estan en TraceRecords.csv (ya no se devuelven
        // las 1,500 filas en la respuesta HTTP).
    }

    public class E7Provenance
    {
        public string ModelAssemblyName { get; set; } = "";

        public string ModelAssemblyVersion { get; set; } = "";

        public string ModelAssemblySha256 { get; set; } = "";

        public string ModelAssemblyPath { get; set; } = "";

        public string MWArrayAssemblyVersion { get; set; } = "";

        public string MatlabRuntimeEnvironment { get; set; } = "";

        public string DotNetRuntime { get; set; } = "";

        public string OperatingSystem { get; set; } = "";

        public string ProcessArchitecture { get; set; } = "";

        public string MachineName { get; set; } = "";

        public int ProcessorCount { get; set; }

        public double? MatlabServiceInitializationMs { get; set; }

        public DateTime CapturedAtUtc { get; set; }
    }

    public class StudyReproducibility
    {
        public int StudyId { get; set; }

        public int Executions { get; set; }

        public int Successful { get; set; }

        public int DistinctHashes { get; set; }

        public string ReferenceHash { get; set; } = "";
    }
}
