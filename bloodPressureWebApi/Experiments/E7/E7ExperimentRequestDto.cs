namespace bloodPressureWebApi.Experiments.E7
{
    public class E7ExperimentRequestDto
    {
        public List<int>? StudyIds { get; set; }

        public int Iterations { get; set; } = 30;

        public bool ExportCsv { get; set; } = true;
    }
}