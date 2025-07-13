using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class PatientRiskFactors
{
    [Key]
    public int PatientRiskFactorId { get; set; }

    public int UserId { get; set; }

    public int RiskFactorId { get; set; }

    [ForeignKey("RiskFactorId")]
    [InverseProperty("PatientRiskFactors")]
    public virtual RiskFactors RiskFactor { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("PatientRiskFactors")]
    public virtual Users User { get; set; } = null!;
}
