namespace bloodPressureWebApi.Experiments.E5
{
    public class E5ExperimentResultDto
    {
        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public int StudiesProcessed { get; set; }

        public int Iterations { get; set; }

        public string OutputFolder { get; set; } = string.Empty;

        public List<E5ScenarioSummary> Scenarios { get; set; } = new();

        /// <summary>NUEVO: escenarios ejecutados a traves de la cola con reintentos.</summary>
        public List<E5QueueRecoverySummary> QueueRecovery { get; set; } = new();

        public double TotalExecutionTimeSeconds { get; set; }

        /// <summary>Errores NO inyectados (inesperados). Deberia quedar vacio.</summary>
        public List<string> UnexpectedErrors { get; set; } = new();
    }

    public class E5QueueRecoverySummary
    {
        public string Scenario { get; set; } = string.Empty;

        public int Jobs { get; set; }

        /// <summary>Jobs que terminaron con exito (al primer intento o tras reintentos).</summary>
        public int EventuallySuccessful { get; set; }

        /// <summary>Jobs que necesitaron al menos un reintento y terminaron con exito.</summary>
        public int RecoveredByRetry { get; set; }

        public int FinallyFailed { get; set; }

        public double MeanAttempts { get; set; }

        public int MaxAttempts { get; set; }

        public int SuccessfulMatchingBaseline { get; set; }

        public double? MedianMakespanMs { get; set; }
    }

    public class E5ScenarioSummary
    {
        public string Scenario { get; set; } = string.Empty;

        public int Executions { get; set; }

        public int Successful { get; set; }

        public int Failed { get; set; }

        public double SuccessRatePercent { get; set; }

        /// <summary>Fallos cuya excepcion corresponde a la inyectada.</summary>
        public int FailuresMatchingInjectedFault { get; set; }

        /// <summary>Ejecuciones exitosas cuyo resultado == linea base.</summary>
        public int SuccessfulMatchingBaseline { get; set; }

        public double? MedianSuccessMs { get; set; }

        public double? MedianFailureMs { get; set; }

        /// <summary>
        /// Pasada limpia posterior al escenario: estudios cuyo resultado
        /// coincide con la linea base (evidencia de que el fallo no dejo
        /// estado corrupto). Debe ser igual al numero de estudios.
        /// </summary>
        public int PostFaultVerificationMatches { get; set; }

        public int PostFaultVerificationStudies { get; set; }
    }
}
