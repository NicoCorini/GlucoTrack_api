using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class ChangeLogs
{
    [Key]
    public int ChangeLogId { get; set; }

    public int DoctorId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string TableName { get; set; } = null!;

    public int RecordId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Action { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime Timestamp { get; set; }

    public string? DetailsBefore { get; set; }

    public string? DetailsAfter { get; set; }

    [ForeignKey("DoctorId")]
    [InverseProperty("ChangeLogs")]
    public virtual Users Doctor { get; set; } = null!;
}
