using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

[Index("Label", Name = "UQ__Measurem__EDBE0C582955880A", IsUnique = true)]
public partial class MeasurementTypes
{
    [Key]
    public int MeasurementTypeId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string Label { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    [InverseProperty("MeasurementType")]
    public virtual ICollection<GlycemicMeasurements> GlycemicMeasurements { get; set; } = new List<GlycemicMeasurements>();
}
