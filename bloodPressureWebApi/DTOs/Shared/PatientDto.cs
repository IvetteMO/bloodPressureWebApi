namespace bloodPressureWebApi.DTOs.Shared
{
        public class PatientDto
        {
            public string? FirstName
            { get; set; }

            public string? LastName
            { get; set; }

            public DateTime? BirthDate
            { get; set; }

            public byte? Sex
            { get; set; }

            public string? PhoneNumber
            { get; set; }

            public string? Email
            { get; set; }

            public string? Notes
            { get; set; }
        }
    }

