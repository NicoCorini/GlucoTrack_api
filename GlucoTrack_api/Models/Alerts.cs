using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class Alerts
{
    [Key]
    public int AlertId { get; set; }

    public int AlertTypeId { get; set; }

    public int UserId { get; set; }

    public string Message { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    public DateOnly? ReferenceDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? ReferencePeriod { get; set; }

    public int? ReferenceObjectId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? ResolvedAt { get; set; }

    [InverseProperty("Alert")]
    public virtual ICollection<AlertRecipients> AlertRecipients { get; set; } = new List<AlertRecipients>();

    [ForeignKey("AlertTypeId")]
    [InverseProperty("Alerts")]
    public virtual AlertTypes AlertType { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Alerts")]
    public virtual Users User { get; set; } = null!;
}
