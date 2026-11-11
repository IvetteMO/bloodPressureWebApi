namespace bloodPressureWebApi.DTOs.Patients
{
    public class PatientListDto
    {
        public int PatientId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public int TotalStudies { get; set; }
    }
}
