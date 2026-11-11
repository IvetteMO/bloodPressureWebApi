namespace bloodPressureWebApi.DTOs.E1
{
    public class E1MatlabReferenceDto
    {
        public string FileName { get; set; }
            = string.Empty;

        public int Age { get; set; }

        // ==========================
        // ANN
        // ==========================

        public double SysANN { get; set; }

        public double DiaANN { get; set; }

        public double PulseANN { get; set; }

        // ==========================
        // FUZZY
        // ==========================

        public double BPLevelOutput { get; set; }

        public double HeartRateOutput { get; set; }

        // ==========================
        // DAY/NIGHT MEANS
        // ==========================

        public double Sdia { get; set; }

        public double Ddia { get; set; }

        public double Snoche { get; set; }

        public double Dnoche { get; set; }

        // ==========================
        // NOCTURNAL
        // ==========================

        public double Csis { get; set; }

        public double Cdia { get; set; }

        public double NocturnalOutput { get; set; }

        public string NocturnalProfile { get; set; }
            = string.Empty;

        // ==========================
        // EXECUTION
        // ==========================

        public string Status { get; set; }
            = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}