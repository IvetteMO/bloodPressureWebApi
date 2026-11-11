namespace bloodPressureWebApi.Experiments.E4
{
    public class E4ExperimentRequestDto
    {
        public List<int>? StudyIds { get; set; }

        public int Iterations { get; set; } = 10;

        /// <summary>Consumidores de la cola en modo Async (produccion = 1).</summary>
        public int AsyncConsumers { get; set; } = 1;

        public bool ExportCsv { get; set; } = true;
    }
}
