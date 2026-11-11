using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bloodPressureWebApi.Models
{
    public class ExperimentRun
    {
        [Key]
        public long ExperimentRunId { get; set; }

        // =========================================
        // EXPERIMENT IDENTIFICATION
        // =========================================

        [Required]
        [MaxLength(20)]
        public string ExperimentCode { get; set; }
            = string.Empty;

        public Guid BatchId { get; set; }

        public int StudyId { get; set; }

        public int Repetition { get; set; }

        public bool IsWarmup { get; set; }

        [MaxLength(50)]
        public string ExecutionMode { get; set; }
            = "SteadyState";

        // =========================================
        // TIMING
        // =========================================

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public double TotalMilliseconds { get; set; }

        // =========================================
        // MEMORY
        // =========================================

        public double MemoryBeforeMB { get; set; }

        public double MemoryAfterMB { get; set; }

        public double MemoryDeltaMB { get; set; }

        // =========================================
        // RESULT
        // =========================================

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        // =========================================
        // TRACEABILITY
        // =========================================

        public string? ModelVersion { get; set; }

        public string? FeaturePipelineVersion { get; set; }

        public string? MatlabRuntimeVersion { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
    }
}