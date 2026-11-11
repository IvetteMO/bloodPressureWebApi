namespace bloodPressureWebApi.DTOs.Experiments
{
    public class ExperimentalImportRequestDto
    {
        public string FolderPath { get; set; }
            = string.Empty;

        public TimeSpan DefaultAwakeTime { get; set; }
            = new TimeSpan(6, 0, 0);

        public TimeSpan DefaultSleepTime { get; set; }
            = new TimeSpan(23, 0, 0);
    }
}