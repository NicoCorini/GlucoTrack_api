using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class MedicationIntakes
{
    [Key]
    public int MedicationIntakeId { get; set; }

    public int UserId { get; set; }

    public int? MedicationScheduleId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime IntakeDateTime { get; set; }

    [Column(TypeName = "numeric(8, 2)")]
    public decimal ExpectedQuantityValue { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string Unit { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Note { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? MedicationTakenName { get; set; }

    [ForeignKey("MedicationScheduleId")]
    [InverseProperty("MedicationIntakes")]
    public virtual MedicationSchedules? MedicationSchedule { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("MedicationIntakes")]
    public virtual Users User { get; set; } = null!;
}
