using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

[Index("RoleName", Name = "UQ__Roles__8A2B6160343D1A82", IsUnique = true)]
public partial class Roles
{
    [Key]
    public int RoleId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string RoleName { get; set; } = null!;

    [InverseProperty("Role")]
    public virtual ICollection<Users> Users { get; set; } = new List<Users>();
}
