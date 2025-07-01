using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class GlycemicMeasurements
{
    [Key]
    public int GlycemicMeasurementId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime MeasurementDateTime { get; set; }

    public int MeasurementTypeId { get; set; }

    public int MealTypeId { get; set; }

    public short Value { get; set; }

    [Column(TypeName = "text")]
    public string? Note { get; set; }

    [ForeignKey("MealTypeId")]
    [InverseProperty("GlycemicMeasurements")]
    public virtual MealTypes MealType { get; set; } = null!;

    [ForeignKey("MeasurementTypeId")]
    [InverseProperty("GlycemicMeasurements")]
    public virtual MeasurementTypes MeasurementType { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("GlycemicMeasurements")]
    public virtual Users User { get; set; } = null!;
}
