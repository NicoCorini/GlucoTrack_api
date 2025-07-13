using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

[Index("Label", Name = "UQ__RiskFact__EDBE0C582318A573", IsUnique = true)]
public partial class RiskFactors
{
    [Key]
    public int RiskFactorId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string Label { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    [InverseProperty("RiskFactor")]
    public virtual ICollection<PatientRiskFactors> PatientRiskFactors { get; set; } = new List<PatientRiskFactors>();
}
