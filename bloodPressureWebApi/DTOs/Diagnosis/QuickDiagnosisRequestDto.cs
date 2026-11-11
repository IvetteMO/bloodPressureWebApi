namespace bloodPressureWebApi.DTOs.Diagnosis
{
    public class QuickDiagnosisRequestDto
    {
        public int Age { get; set; }

        public double Systolic1 { get; set; }

        public double Systolic2 { get; set; }

        public double Systolic3 { get; set; }

        public double Diastolic1 { get; set; }

        public double Diastolic2 { get; set; }

        public double Diastolic3 { get; set; }

        public double Pulse1 { get; set; }

        public double Pulse2 { get; set; }

        public double Pulse3 { get; set; }
    }
}