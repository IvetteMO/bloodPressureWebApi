namespace bloodPressureWebApi.DTOs.Studies
{
    public class CreateStudyDto
    {
        public int PatientId { get; set; }

        public TimeSpan? AwakeTime { get; set; }

        public TimeSpan? SleepTime { get; set; }
    }
}