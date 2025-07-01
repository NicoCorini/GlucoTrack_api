using System;
using System.Collections.Generic;
using GlucoTrack_api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Data;

public partial class GlucoTrackDBContext : DbContext
{
    public GlucoTrackDBContext(DbContextOptions<GlucoTrackDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AlertRecipients> AlertRecipients { get; set; }

    public virtual DbSet<AlertTypes> AlertTypes { get; set; }

    public virtual DbSet<Alerts> Alerts { get; set; }

    public virtual DbSet<ChangeLogs> ChangeLogs { get; set; }

    public virtual DbSet<DiagnosedDiseases> DiagnosedDiseases { get; set; }

    public virtual DbSet<GlycemicMeasurements> GlycemicMeasurements { get; set; }

    public virtual DbSet<MealTypes> MealTypes { get; set; }

    public virtual DbSet<MeasurementTypes> MeasurementTypes { get; set; }

    public virtual DbSet<MedicationIntakes> MedicationIntakes { get; set; }

    public virtual DbSet<MedicationSchedules> MedicationSchedules { get; set; }

    public virtual DbSet<PatientComorbidities> PatientComorbidities { get; set; }

    public virtual DbSet<PatientDoctors> PatientDoctors { get; set; }

    public virtual DbSet<PatientRiskFactors> PatientRiskFactors { get; set; }

    public virtual DbSet<RiskFactors> RiskFactors { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Symptoms> Symptoms { get; set; }

    public virtual DbSet<Therapies> Therapies { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertRecipients>(entity =>
        {
            entity.HasKey(e => e.AlertRecipientId).HasName("PK__AlertRec__51A78A67B8D05DDE");

            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.Alert).WithMany(p => p.AlertRecipients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlertReci__Alert__6E01572D");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.AlertRecipients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlertReci__Recip__6EF57B66");
        });

        modelBuilder.Entity<AlertTypes>(entity =>
        {
            entity.HasKey(e => e.AlertTypeId).HasName("PK__AlertTyp__016D41BD9997DFF9");
        });

        modelBuilder.Entity<Alerts>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__Alerts__EBB16A8DA181AA6A");

            entity.HasOne(d => d.AlertType).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Alerts__AlertTyp__6A30C649");

            entity.HasOne(d => d.User).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Alerts__UserId__6B24EA82");
        });

        modelBuilder.Entity<ChangeLogs>(entity =>
        {
            entity.HasKey(e => e.ChangeLogId).HasName("PK__ChangeLo__6AD2E8C7D566AB79");

            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Doctor).WithMany(p => p.ChangeLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChangeLog__Docto__72C60C4A");
        });

        modelBuilder.Entity<DiagnosedDiseases>(entity =>
        {
            entity.HasKey(e => e.DiseaseId).HasName("PK__Diagnose__69B53389C2CE42CF");

            entity.HasOne(d => d.User).WithMany(p => p.DiagnosedDiseases)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Diagnosed__UserI__6477ECF3");
        });

        modelBuilder.Entity<GlycemicMeasurements>(entity =>
        {
            entity.HasKey(e => e.GlycemicMeasurementId).HasName("PK__Glycemic__A931B6854F628508");

            entity.HasOne(d => d.MealType).WithMany(p => p.GlycemicMeasurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GlycemicM__MealT__5EBF139D");

            entity.HasOne(d => d.MeasurementType).WithMany(p => p.GlycemicMeasurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GlycemicM__Measu__5DCAEF64");

            entity.HasOne(d => d.User).WithMany(p => p.GlycemicMeasurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GlycemicM__UserI__5CD6CB2B");
        });

        modelBuilder.Entity<MealTypes>(entity =>
        {
            entity.HasKey(e => e.MealTypeId).HasName("PK__MealType__702B379E68FF382C");
        });

        modelBuilder.Entity<MeasurementTypes>(entity =>
        {
            entity.HasKey(e => e.MeasurementTypeId).HasName("PK__Measurem__167933E7F51C2F9B");
        });

        modelBuilder.Entity<MedicationIntakes>(entity =>
        {
            entity.HasKey(e => e.MedicationIntakeId).HasName("PK__Medicati__698AF4E52C02331D");

            entity.HasOne(d => d.MedicationSchedule).WithMany(p => p.MedicationIntakes).HasConstraintName("FK__Medicatio__Medic__5441852A");

            entity.HasOne(d => d.User).WithMany(p => p.MedicationIntakes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Medicatio__UserI__534D60F1");
        });

        modelBuilder.Entity<MedicationSchedules>(entity =>
        {
            entity.HasKey(e => e.MedicationScheduleId).HasName("PK__Medicati__EDCDE99CB72480AA");

            entity.HasOne(d => d.Therapy).WithMany(p => p.MedicationSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Medicatio__Thera__5070F446");
        });

        modelBuilder.Entity<PatientComorbidities>(entity =>
        {
            entity.HasKey(e => e.PatientComorbidityId).HasName("PK__PatientC__30DF7E516AE54D5C");

            entity.HasOne(d => d.User).WithMany(p => p.PatientComorbidities)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientCo__UserI__49C3F6B7");
        });

        modelBuilder.Entity<PatientDoctors>(entity =>
        {
            entity.HasKey(e => e.PatientDoctorId).HasName("PK__PatientD__2BA4595542954319");

            entity.HasOne(d => d.Doctor).WithMany(p => p.PatientDoctorsDoctor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientDo__Docto__403A8C7D");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientDoctorsPatient)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientDo__Patie__3F466844");
        });

        modelBuilder.Entity<PatientRiskFactors>(entity =>
        {
            entity.HasKey(e => e.PatientRiskFactorId).HasName("PK__PatientR__9562BCDFDB0652D7");

            entity.HasOne(d => d.RiskFactor).WithMany(p => p.PatientRiskFactors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientRi__RiskF__46E78A0C");

            entity.HasOne(d => d.User).WithMany(p => p.PatientRiskFactors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientRi__UserI__45F365D3");
        });

        modelBuilder.Entity<RiskFactors>(entity =>
        {
            entity.HasKey(e => e.RiskFactorId).HasName("PK__RiskFact__7C28B9146237A4FD");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AB6A9D0AC");
        });

        modelBuilder.Entity<Symptoms>(entity =>
        {
            entity.HasKey(e => e.SymptomId).HasName("PK__Symptoms__D26ED89673E0B71A");

            entity.HasOne(d => d.User).WithMany(p => p.Symptoms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Symptoms__UserId__619B8048");
        });

        modelBuilder.Entity<Therapies>(entity =>
        {
            entity.HasKey(e => e.TherapyId).HasName("PK__Therapie__2D1FD1E2C2A9AB13");

            entity.HasOne(d => d.Doctor).WithMany(p => p.TherapiesDoctor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Therapies__Docto__4CA06362");

            entity.HasOne(d => d.User).WithMany(p => p.TherapiesUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Therapies__UserI__4D94879B");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CA1696C10");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
