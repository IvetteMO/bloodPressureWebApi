namespace bloodPressureWebApi.Experiments.E2
{
    public class E2ExperimentRequestDto
    {
        public List<int>? StudyIds { get; set; }

        public int Iterations { get; set; } = 1;

        public bool ExportCsv { get; set; } = true;
    }
}
