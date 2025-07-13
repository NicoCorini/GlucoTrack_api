using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class Symptoms
{
    [Key]
    public int SymptomId { get; set; }

    public int UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Description { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime OccurredAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Symptoms")]
    public virtual Users User { get; set; } = null!;
}
