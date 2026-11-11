using Microsoft.EntityFrameworkCore;
using bloodPressureWebApi.Models;


namespace bloodPressureWebApi.Data
{
    public class ApplicationDbContext
        : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================================================
        // TABLES
        // =====================================================

        public DbSet<Patients> Patients { get; set; }
        public DbSet<AuditLog>AuditLogs { get; set; }

        public DbSet<PatientRiskFactor>
            PatientRiskFactors
        { get; set; }

        public DbSet<MAPAStudy>
            MAPAStudies
        { get; set; }

        public DbSet<MAPAReading>
            MAPAReadings
        { get; set; }

        public DbSet<MAPAFeature>
            MAPAFeatures
        { get; set; }

        public DbSet<AIDiagnosis>
            AIDiagnosis
        { get; set; }

        public DbSet<ExperimentRun>
    ExperimentRuns
        { get; set; }

        public DbSet<E1BValidationResult> E1BValidationResults
        { get; set; }

        public DbSet<MAPAValidationResult>
    MAPAValidationResults
        { get; set; }

        // =====================================================
        // MODEL CONFIGURATION
        // =====================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================
            // PATIENTS
            // =================================================

            modelBuilder.Entity<Patients>()
                .HasMany(x => x.Studies)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // STUDY → RISK FACTORS
            // =================================================

            modelBuilder.Entity<MAPAStudy>()
                .HasMany(x => x.RiskFactors)
                .WithOne(x => x.Study)
                .HasForeignKey(x => x.StudyId)
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // STUDY → READINGS
            // =================================================

            modelBuilder.Entity<MAPAStudy>()
                .HasMany(x => x.Readings)
                .WithOne(x => x.Study)
                .HasForeignKey(x => x.StudyId)
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // STUDY → FEATURES (1:1)
            // =================================================

            modelBuilder.Entity<MAPAStudy>()
                .HasOne(x => x.Features)
                .WithOne(x => x.Study)
                .HasForeignKey<MAPAFeature>(
                    x => x.StudyId)
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // STUDY → DIAGNOSIS (1:1)
            // =================================================

            modelBuilder.Entity<MAPAStudy>()
                .HasOne(x => x.Diagnosis)
                .WithOne(x => x.Study)
                .HasForeignKey<AIDiagnosis>(
                    x => x.StudyId)
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // UNIQUE FEATURE PER STUDY
            // =================================================

            modelBuilder.Entity<MAPAFeature>()
                .HasIndex(x => x.StudyId)
                .IsUnique();

            // =================================================
            // UNIQUE DIAGNOSIS PER STUDY
            // =================================================

            modelBuilder.Entity<AIDiagnosis>()
                .HasIndex(x => x.StudyId)
                .IsUnique();

            // =================================================
            // AVOID DUPLICATE READINGS
            // =================================================

            modelBuilder.Entity<MAPAReading>()
                .HasIndex(x =>
                    new
                    {
                        x.StudyId,
                        x.ReadingDateTime
                    });
                

            // =================================================
            // DECIMAL PRECISION
            // =================================================

            modelBuilder.Entity<MAPAFeature>()
                .Property(x => x.SystolicNightRatio).HasColumnType("float");

            modelBuilder.Entity<MAPAFeature>()
                .Property(x => x.DiastolicNightRatio)
                .HasColumnType("float");

            modelBuilder.Entity<AIDiagnosis>()
                .Property(x => x.HeartAge)
                .HasColumnType("float");

            // =================================================
            // DEFAULT VALUES
            // =================================================

            modelBuilder.Entity<MAPAStudy>()
                .Property(x => x.StudyStatus)
                .HasDefaultValue("IMPORTED");

            modelBuilder.Entity<MAPAStudy>()
                .Property(x => x.CustomSchedule)
                .HasDefaultValue(false);
        }
    }
}