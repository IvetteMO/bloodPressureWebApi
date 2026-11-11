namespace bloodPressureWebApi.Experiments.E3
{
    public class E3ExperimentRequestDto
    {
        /// <summary>
        /// Niveles de concurrencia a evaluar en una sola corrida,
        /// p. ej. [1, 2, 4, 8]. El nivel 1 es la linea base secuencial.
        /// Si viene vacio se usa Concurrency.
        /// </summary>
        public List<int>? ConcurrencyLevels { get; set; }

        /// <summary>Compatibilidad: un solo nivel.</summary>
        public int Concurrency { get; set; } = 1;

        public int Iterations { get; set; } = 5;

        /// <summary>
        /// true (recomendado): antes de medir se ejecuta una pasada completa
        /// secuencial NO medida, para que E3 mida concurrencia en estado
        /// estable y no mezcle el arranque en frio (que ya caracteriza E2).
        /// </summary>
        public bool WarmStart { get; set; } = true;

        public List<int>? StudyIds { get; set; }
    }
}
