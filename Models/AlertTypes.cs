using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

[Index("Label", Name = "UQ__AlertTyp__EDBE0C58E5370424", IsUnique = true)]
public partial class AlertTypes
{
    [Key]
    public int AlertTypeId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Label { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    [InverseProperty("AlertType")]
    public virtual ICollection<Alerts> Alerts { get; set; } = new List<Alerts>();
}
