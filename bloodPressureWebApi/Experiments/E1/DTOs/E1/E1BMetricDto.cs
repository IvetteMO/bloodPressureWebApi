namespace bloodPressureWebApi.DTOs.E1
{
    public class E1BMetricDto
    {
        public string Metric { get; set; }
            = string.Empty;

        public string Unit { get; set; }
            = string.Empty;

        public int N { get; set; }

        public double Mae { get; set; }

        public double Rmse { get; set; }

        public double MeanBias { get; set; }

        public double? Mape { get; set; }

        public double StandardDeviation { get; set; }

        public double PearsonR { get; set; }

        public double? MedianAbsoluteError { get; set; }

        public double LowerAgreementLimit { get; set; }

        public double UpperAgreementLimit { get; set; }

        public double MaxAbsoluteError { get; set; }
    }
}