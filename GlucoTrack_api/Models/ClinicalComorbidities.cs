using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class ClinicalComorbidities
{
    [Key]
    public int ClinicalComorbidityId { get; set; }

    public int UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string Comorbidity { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ClinicalComorbidities")]
    public virtual Users User { get; set; } = null!;
}
