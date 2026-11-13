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

        public DbSet<AppUser> AppUsers { get; set; }

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

            // =====================================
            // USUARIOS (login)
            // =====================================

            modelBuilder.Entity<AppUser>(user =>
            {
                user.HasKey(x => x.AppUserId);

                user.HasIndex(x => x.Email).IsUnique();

                user.Property(x => x.Email).HasMaxLength(256).IsRequired();
                user.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
                user.Property(x => x.PasswordHash).IsRequired();
                user.Property(x => x.Role).HasMaxLength(30).IsRequired();
                user.Property(x => x.SecurityStamp).HasMaxLength(64).IsRequired();
            });

            // =====================================
            // AUDITORIA: columnas de usuario
            // =====================================

            modelBuilder.Entity<AuditLog>(audit =>
            {
                audit.Property(x => x.UserEmail).HasMaxLength(256);
                audit.Property(x => x.UserRole).HasMaxLength(30);
                audit.Property(x => x.HttpMethod).HasMaxLength(10);
                audit.Property(x => x.Path).HasMaxLength(500);
                audit.Property(x => x.IpAddress).HasMaxLength(64);

                audit.HasIndex(x => x.CreatedAt);
                audit.HasIndex(x => x.UserId);
            });

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