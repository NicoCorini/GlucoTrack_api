using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class MedicationSchedules
{
    [Key]
    public int MedicationScheduleId { get; set; }

    public int TherapyId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string MedicationName { get; set; } = null!;

    [Column(TypeName = "numeric(8, 2)")]
    public decimal ExpectedQuantity { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string ExpectedUnit { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime ScheduledDateTime { get; set; }

    [InverseProperty("MedicationSchedule")]
    public virtual ICollection<MedicationIntakes> MedicationIntakes { get; set; } = new List<MedicationIntakes>();

    [ForeignKey("TherapyId")]
    [InverseProperty("MedicationSchedules")]
    public virtual Therapies Therapy { get; set; } = null!;
}
