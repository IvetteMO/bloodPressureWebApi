using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bloodPressureWebApi.Models
{
    public class MAPAReading
    {
        [Key]
        public int ReadingId { get; set; }

        [ForeignKey("Study")]
        public int StudyId { get; set; }

        public DateTime ReadingDateTime { get; set; }

        public int Systolic { get; set; }

        public int Diastolic { get; set; }

        public int Pulse { get; set; }

        // DAY / NIGHT
        public string? Period { get; set; }

        // ====================================
        // NAVIGATION
        // ====================================

        public MAPAStudy? Study { get; set; }
    }
}