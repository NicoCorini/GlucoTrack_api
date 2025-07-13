using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

[Index("Username", Name = "UQ__Users__536C85E4E7CB9FC3", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D1053417C6A854", IsUnique = true)]
public partial class Users
{
    [Key]
    public int UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string FirstName { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string LastName { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    public int RoleId { get; set; }

    public DateOnly? BirthDate { get; set; }

    [Column(TypeName = "numeric(5, 2)")]
    public decimal? Height { get; set; }

    [Column(TypeName = "numeric(5, 2)")]
    public decimal? Weight { get; set; }

    [StringLength(16)]
    [Unicode(false)]
    public string? FiscalCode { get; set; }

    [StringLength(100)]
    public string? Gender { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Specialization { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? AffiliatedHospital { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastAccess { get; set; }

    [InverseProperty("RecipientUser")]
    public virtual ICollection<AlertRecipients> AlertRecipients { get; set; } = new List<AlertRecipients>();

    [InverseProperty("User")]
    public virtual ICollection<Alerts> Alerts { get; set; } = new List<Alerts>();

    [InverseProperty("Doctor")]
    public virtual ICollection<ChangeLogs> ChangeLogs { get; set; } = new List<ChangeLogs>();

    [InverseProperty("User")]
    public virtual ICollection<ClinicalComorbidities> ClinicalComorbidities { get; set; } = new List<ClinicalComorbidities>();

    [InverseProperty("User")]
    public virtual ICollection<GlycemicMeasurements> GlycemicMeasurements { get; set; } = new List<GlycemicMeasurements>();

    [InverseProperty("User")]
    public virtual ICollection<MedicationIntakes> MedicationIntakes { get; set; } = new List<MedicationIntakes>();

    [InverseProperty("Doctor")]
    public virtual ICollection<PatientDoctors> PatientDoctorsDoctor { get; set; } = new List<PatientDoctors>();

    [InverseProperty("Patient")]
    public virtual ICollection<PatientDoctors> PatientDoctorsPatient { get; set; } = new List<PatientDoctors>();

    [InverseProperty("User")]
    public virtual ICollection<PatientRiskFactors> PatientRiskFactors { get; set; } = new List<PatientRiskFactors>();

    [InverseProperty("User")]
    public virtual ICollection<ReportedConditions> ReportedConditions { get; set; } = new List<ReportedConditions>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Roles Role { get; set; } = null!;

    [InverseProperty("User")]
    public virtual ICollection<Symptoms> Symptoms { get; set; } = new List<Symptoms>();

    [InverseProperty("Doctor")]
    public virtual ICollection<Therapies> TherapiesDoctor { get; set; } = new List<Therapies>();

    [InverseProperty("User")]
    public virtual ICollection<Therapies> TherapiesUser { get; set; } = new List<Therapies>();
}
