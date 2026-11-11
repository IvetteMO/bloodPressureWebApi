namespace bloodPressureWebApi.Queues
{
    /// <summary>
    /// Numero de consumidores de la cola de diagnostico.
    ///
    /// El worker arranca MaxConsumers ciclos de consumo, pero solo los primeros
    /// ActiveConsumers toman trabajos. Asi E4/E6 pueden variar el numero de
    /// consumidores en tiempo de ejecucion sin reiniciar la API.
    /// En produccion el valor por defecto es 1 (comportamiento original).
    /// </summary>
    public class DiagnosisWorkerOptions
    {
        public int MaxConsumers { get; init; } = 8;

        private int _activeConsumers = 1;

        public int ActiveConsumers
        {
            get => Volatile.Read(ref _activeConsumers);
            set => Volatile.Write(ref _activeConsumers,
                Math.Clamp(value, 1, MaxConsumers));
        }

        /// <summary>Reintentos maximos por job (comportamiento original: 3).</summary>
        public int MaxRetries { get; set; } = 3;
    }
}
