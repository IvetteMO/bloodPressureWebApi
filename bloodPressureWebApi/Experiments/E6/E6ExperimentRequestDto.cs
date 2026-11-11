namespace bloodPressureWebApi.Experiments.E6
{
    public class E6ExperimentRequestDto
    {
        public List<int>? StudyIds { get; set; }

        /// <summary>Iteraciones por configuracion (unidad = lote completo).</summary>
        public int Iterations { get; set; } = 10;

        public bool IncludeSync { get; set; } = true;

        /// <summary>Consumidores de la cola a evaluar en modo Async.</summary>
        public List<int>? AsyncConsumers { get; set; }

        public bool ExportCsv { get; set; } = true;
    }
}
