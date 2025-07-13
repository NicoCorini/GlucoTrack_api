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

    public virtual DbSet<ClinicalComorbidities> ClinicalComorbidities { get; set; }

    public virtual DbSet<GlycemicMeasurements> GlycemicMeasurements { get; set; }

    public virtual DbSet<MealTypes> MealTypes { get; set; }

    public virtual DbSet<MeasurementTypes> MeasurementTypes { get; set; }

    public virtual DbSet<MedicationIntakes> MedicationIntakes { get; set; }

    public virtual DbSet<MedicationSchedules> MedicationSchedules { get; set; }

    public virtual DbSet<PatientDoctors> PatientDoctors { get; set; }

    public virtual DbSet<PatientRiskFactors> PatientRiskFactors { get; set; }

    public virtual DbSet<ReportedConditions> ReportedConditions { get; set; }

    public virtual DbSet<RiskFactors> RiskFactors { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Symptoms> Symptoms { get; set; }

    public virtual DbSet<Therapies> Therapies { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertRecipients>(entity =>
        {
            entity.HasKey(e => e.AlertRecipientId).HasName("PK__AlertRec__51A78A67CD2003D6");

            entity.HasOne(d => d.Alert).WithMany(p => p.AlertRecipients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlertReci__Alert__6FE99F9F");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.AlertRecipients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlertReci__Recip__70DDC3D8");
        });

        modelBuilder.Entity<AlertTypes>(entity =>
        {
            entity.HasKey(e => e.AlertTypeId).HasName("PK__AlertTyp__016D41BDBEC57C1D");
        });

        modelBuilder.Entity<Alerts>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__Alerts__EBB16A8D93342C07");

            entity.Property(e => e.Status).HasDefaultValue("open");

            entity.HasOne(d => d.AlertType).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Alerts__AlertTyp__6B24EA82");

            entity.HasOne(d => d.User).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Alerts__UserId__6C190EBB");
        });

        modelBuilder.Entity<ChangeLogs>(entity =>
        {
            entity.HasKey(e => e.ChangeLogId).HasName("PK__ChangeLo__6AD2E8C734B3FB44");

            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Doctor).WithMany(p => p.ChangeLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChangeLog__Docto__74AE54BC");
        });

        modelBuilder.Entity<ClinicalComorbidities>(entity =>
        {
            entity.HasKey(e => e.ClinicalComorbidityId).HasName("PK__Clinical__339FB25E569EDA8E");

            entity.HasOne(d => d.User).WithMany(p => p.ClinicalComorbidities)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClinicalC__UserI__49C3F6B7");
        });

        modelBuilder.Entity<GlycemicMeasurements>(entity =>
        {
            entity.HasKey(e => e.GlycemicMeasurementId).HasName("PK__Glycemic__A931B685F4044CA1");

            entity.HasOne(d => d.MealType).WithMany(p => p.GlycemicMeasurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GlycemicM__MealT__5FB337D6");

            entity.HasOne(d => d.MeasurementType).WithMany(p => p.GlycemicMeasurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GlycemicM__Measu__5EBF139D");

            entity.HasOne(d => d.User).WithMany(p => p.GlycemicMeasurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GlycemicM__UserI__5DCAEF64");
        });

        modelBuilder.Entity<MealTypes>(entity =>
        {
            entity.HasKey(e => e.MealTypeId).HasName("PK__MealType__702B379E70CFC279");
        });

        modelBuilder.Entity<MeasurementTypes>(entity =>
        {
            entity.HasKey(e => e.MeasurementTypeId).HasName("PK__Measurem__167933E77BBD89D0");
        });

        modelBuilder.Entity<MedicationIntakes>(entity =>
        {
            entity.HasKey(e => e.MedicationIntakeId).HasName("PK__Medicati__698AF4E5852C34BE");

            entity.HasOne(d => d.MedicationSchedule).WithMany(p => p.MedicationIntakes).HasConstraintName("FK__Medicatio__Medic__5535A963");

            entity.HasOne(d => d.User).WithMany(p => p.MedicationIntakes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Medicatio__UserI__5441852A");
        });

        modelBuilder.Entity<MedicationSchedules>(entity =>
        {
            entity.HasKey(e => e.MedicationScheduleId).HasName("PK__Medicati__EDCDE99C77C2FF9A");

            entity.HasOne(d => d.Therapy).WithMany(p => p.MedicationSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Medicatio__Thera__5165187F");
        });

        modelBuilder.Entity<PatientDoctors>(entity =>
        {
            entity.HasKey(e => e.PatientDoctorId).HasName("PK__PatientD__2BA45955C2E8943F");

            entity.HasOne(d => d.Doctor).WithMany(p => p.PatientDoctorsDoctor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientDo__Docto__403A8C7D");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientDoctorsPatient)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientDo__Patie__3F466844");
        });

        modelBuilder.Entity<PatientRiskFactors>(entity =>
        {
            entity.HasKey(e => e.PatientRiskFactorId).HasName("PK__PatientR__9562BCDFDD7E0AE2");

            entity.HasOne(d => d.RiskFactor).WithMany(p => p.PatientRiskFactors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientRi__RiskF__46E78A0C");

            entity.HasOne(d => d.User).WithMany(p => p.PatientRiskFactors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientRi__UserI__45F365D3");
        });

        modelBuilder.Entity<ReportedConditions>(entity =>
        {
            entity.HasKey(e => e.ConditionId).HasName("PK__Reported__37F5C0CF0A43789E");

            entity.HasOne(d => d.User).WithMany(p => p.ReportedConditions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReportedC__UserI__656C112C");
        });

        modelBuilder.Entity<RiskFactors>(entity =>
        {
            entity.HasKey(e => e.RiskFactorId).HasName("PK__RiskFact__7C28B914584E7464");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A7FFEBFD2");
        });

        modelBuilder.Entity<Symptoms>(entity =>
        {
            entity.HasKey(e => e.SymptomId).HasName("PK__Symptoms__D26ED896538F3769");

            entity.HasOne(d => d.User).WithMany(p => p.Symptoms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Symptoms__UserId__628FA481");
        });

        modelBuilder.Entity<Therapies>(entity =>
        {
            entity.HasKey(e => e.TherapyId).HasName("PK__Therapie__2D1FD1E203A88E65");

            entity.HasOne(d => d.Doctor).WithMany(p => p.TherapiesDoctor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Therapies__Docto__4CA06362");

            entity.HasOne(d => d.PreviousTherapy).WithMany(p => p.InversePreviousTherapy).HasConstraintName("FK__Therapies__Previ__4E88ABD4");

            entity.HasOne(d => d.User).WithMany(p => p.TherapiesUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Therapies__UserI__4D94879B");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C599A4542");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
