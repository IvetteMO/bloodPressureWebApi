namespace bloodPressureWebApi.DTOs.Studies
{
    public class CreateRiskFactorDto
    {
        public int StudyId { get; set; }

        public double? Weight { get; set; }

        public double? Height { get; set; }

        public double? BMI { get; set; }

        public byte? Smoking { get; set; }

        public byte? Diabetes { get; set; }

        public byte? HypertensionTreatment { get; set; }

        public byte? ParentalHypertension { get; set; }
    }
}
