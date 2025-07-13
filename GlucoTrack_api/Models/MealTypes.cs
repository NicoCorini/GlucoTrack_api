using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

[Index("Label", Name = "UQ__MealType__EDBE0C5850839411", IsUnique = true)]
public partial class MealTypes
{
    [Key]
    public int MealTypeId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string Label { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    [InverseProperty("MealType")]
    public virtual ICollection<GlycemicMeasurements> GlycemicMeasurements { get; set; } = new List<GlycemicMeasurements>();
}
