namespace bloodPressureWebApi.DTOs
{
    public class E1ReadingDto
    {
        public int SourceRow { get; set; }

        public DateTime? DateTime { get; set; }

        public TimeSpan? Time { get; set; }

        public double Systolic { get; set; }

        public double Diastolic { get; set; }

        public double Pulse { get; set; }

        public bool IsReplicated { get; set; }

        public int? ReplicatedFromRow { get; set; }
    }
}