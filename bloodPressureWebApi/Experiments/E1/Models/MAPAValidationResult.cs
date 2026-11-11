using System.ComponentModel.DataAnnotations;

namespace bloodPressureWebApi.Models
{
    public class MAPAValidationResult
    {
        [Key]

        public long
            ValidationResultId
        {
            get;
            set;
        }


        // ==========================================
        // EXPERIMENT IDENTIFICATION
        // ==========================================

        public Guid
            BatchId
        {
            get;
            set;
        }


        [MaxLength(30)]

        public string
            ExperimentCode
        {
            get;
            set;
        }
            =
            "E1-A";


        public int
            StudyId
        {
            get;
            set;
        }


        [MaxLength(100)]

        public string
            ExternalPatientCode
        {
            get;
            set;
        }
            =
            string.Empty;


        [MaxLength(255)]

        public string
            SourceFileName
        {
            get;
            set;
        }
            =
            string.Empty;


        // ==========================================
        // LEGACY / TRACEABILITY COUNTS
        //
        // IMPORTANT:
        //
        // These fields are retained only for
        // compatibility with existing database
        // records and previous experimental runs.
        //
        // THEY MUST NOT:
        //
        // - determine the E1 sample
        // - determine concordance
        // - exclude studies
        // - determine how many rows are processed
        //
        // E1 uses the fixed N = 47 protocol.
        // ==========================================

        public int?
            OriginalValidReadings
        {
            get;
            set;
        }


        public int?
            BackendValidReadings
        {
            get;
            set;
        }


        public double?
            ValidReadingsAbsoluteError
        {
            get;
            set;
        }


        // ==========================================
        // DAY SYSTOLIC
        // ==========================================

        public double?
            OriginalDaySystolicMean
        {
            get;
            set;
        }


        public double?
            BackendDaySystolicMean
        {
            get;
            set;
        }


        public double?
            DaySystolicAbsoluteError
        {
            get;
            set;
        }


        public double?
            DaySystolicRelativeError
        {
            get;
            set;
        }


        // ==========================================
        // DAY DIASTOLIC
        // ==========================================

        public double?
            OriginalDayDiastolicMean
        {
            get;
            set;
        }


        public double?
            BackendDayDiastolicMean
        {
            get;
            set;
        }


        public double?
            DayDiastolicAbsoluteError
        {
            get;
            set;
        }


        public double?
            DayDiastolicRelativeError
        {
            get;
            set;
        }


        // ==========================================
        // NIGHT SYSTOLIC
        // ==========================================

        public double?
            OriginalNightSystolicMean
        {
            get;
            set;
        }


        public double?
            BackendNightSystolicMean
        {
            get;
            set;
        }


        public double?
            NightSystolicAbsoluteError
        {
            get;
            set;
        }


        public double?
            NightSystolicRelativeError
        {
            get;
            set;
        }


        // ==========================================
        // NIGHT DIASTOLIC
        // ==========================================

        public double?
            OriginalNightDiastolicMean
        {
            get;
            set;
        }


        public double?
            BackendNightDiastolicMean
        {
            get;
            set;
        }


        public double?
            NightDiastolicAbsoluteError
        {
            get;
            set;
        }


        public double?
            NightDiastolicRelativeError
        {
            get;
            set;
        }


        // ==========================================
        // 24-HOUR MEAN PULSE
        // ==========================================

        public double?
            OriginalMeanPulse
        {
            get;
            set;
        }


        public double?
            BackendMeanPulse
        {
            get;
            set;
        }


        public double?
            MeanPulseAbsoluteError
        {
            get;
            set;
        }


        public double?
            MeanPulseRelativeError
        {
            get;
            set;
        }


        // ==========================================
        // PRESSURE LOAD
        //
        // Retained because these values already
        // exist in the experimental database.
        //
        // Do not include them as independent
        // equivalence metrics unless both values
        // are independently calculated.
        // ==========================================

        public double?
            OriginalDayPressureLoad
        {
            get;
            set;
        }


        public double?
            BackendDayPressureLoad
        {
            get;
            set;
        }


        public double?
            DayPressureLoadAbsoluteError
        {
            get;
            set;
        }


        public double?
            OriginalNightPressureLoad
        {
            get;
            set;
        }


        public double?
            BackendNightPressureLoad
        {
            get;
            set;
        }


        public double?
            NightPressureLoadAbsoluteError
        {
            get;
            set;
        }


        // ==========================================
        // DIPPING
        // ==========================================

        public double?
            OriginalSystolicDippingPercent
        {
            get;
            set;
        }


        public double?
            BackendSystolicDippingPercent
        {
            get;
            set;
        }


        public double?
            SystolicDippingAbsoluteError
        {
            get;
            set;
        }


        public double?
            OriginalDiastolicDippingPercent
        {
            get;
            set;
        }


        public double?
            BackendDiastolicDippingPercent
        {
            get;
            set;
        }


        public double?
            DiastolicDippingAbsoluteError
        {
            get;
            set;
        }


        // ==========================================
        // TRACEABILITY
        // ==========================================

        public DateTime
            CreatedAt
        {
            get;
            set;
        }
            =
            DateTime.Now;
    }
}