namespace bloodPressureWebApi.Models
{
    public class ClinicalNote
    {
        public int ClinicalNoteId { get; set; }

        public int StudyId { get; set; }

        public string Notes { get; set; }

        public string Prescription { get; set; }

        public string Recommendations { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
