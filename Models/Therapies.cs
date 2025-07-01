using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class Therapies
{
    [Key]
    public int TherapyId { get; set; }

    public int DoctorId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "text")]
    public string? Instructions { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("DoctorId")]
    [InverseProperty("TherapiesDoctor")]
    public virtual Users Doctor { get; set; } = null!;

    [InverseProperty("Therapy")]
    public virtual ICollection<MedicationSchedules> MedicationSchedules { get; set; } = new List<MedicationSchedules>();

    [ForeignKey("UserId")]
    [InverseProperty("TherapiesUser")]
    public virtual Users User { get; set; } = null!;
}
