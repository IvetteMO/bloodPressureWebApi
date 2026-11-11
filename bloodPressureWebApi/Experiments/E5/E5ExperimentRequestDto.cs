namespace bloodPressureWebApi.Experiments.E5
{
    public enum FaultScenario
    {
        None = 0,

        // NOTA: MatlabFailure y PersistentFailure se comportan EXACTAMENTE
        // igual (fallan en todas las ejecuciones; solo cambia el mensaje).
        // Se conserva por compatibilidad, pero no se recomienda reportar
        // ambos como escenarios distintos.
        MatlabFailure = 1,

        // Falla cada 5.a ejecucion (20 %), distribuida a lo largo del lote.
        IntermittentFailure = 2,

        // Falla todas las ejecuciones.
        PersistentFailure = 3,

        // Falla las primeras 10 ejecuciones de cada iteracion y luego deja
        // de fallar (rafaga transitoria). El backend NO tiene un mecanismo
        // de reintento: lo que se evalua es que, al cesar la falla, las
        // ejecuciones siguientes vuelvan a producir el resultado correcto.
        RecoveryFailure = 4
    }

    public class E5ExperimentRequestDto
    {
        public List<int>? StudyIds { get; set; }

        public int Iterations { get; set; } = 5;

        /// <summary>
        /// Escenarios a evaluar en una sola corrida. "None" (linea base) se
        /// ejecuta siempre primero aunque no se incluya.
        /// Recomendado: [None, IntermittentFailure, RecoveryFailure, PersistentFailure]
        /// </summary>
        public List<FaultScenario>? Scenarios { get; set; }

        /// <summary>Compatibilidad: un solo escenario.</summary>
        public FaultScenario Scenario { get; set; } = FaultScenario.None;

        /// <summary>
        /// NUEVO: ademas de la llamada directa, ejecutar los escenarios con
        /// fallo A TRAVES DE LA COLA, que si tiene reintentos (hasta 3).
        /// Asi se evalua recuperacion real: fallos transitorios que terminan
        /// en exito gracias al reintento, frente a fallos persistentes.
        /// </summary>
        public bool IncludeQueueRecovery { get; set; } = true;

        public bool ExportCsv { get; set; } = true;
    }
}
