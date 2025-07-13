using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class AlertRecipients
{
    [Key]
    public int AlertRecipientId { get; set; }

    public int AlertId { get; set; }

    public int RecipientUserId { get; set; }

    public bool? IsRead { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ReadAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NotifiedAt { get; set; }

    [ForeignKey("AlertId")]
    [InverseProperty("AlertRecipients")]
    public virtual Alerts Alert { get; set; } = null!;

    [ForeignKey("RecipientUserId")]
    [InverseProperty("AlertRecipients")]
    public virtual Users RecipientUser { get; set; } = null!;
}
