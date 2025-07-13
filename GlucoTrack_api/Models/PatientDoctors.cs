using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Models;

public partial class PatientDoctors
{
    [Key]
    public int PatientDoctorId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [ForeignKey("DoctorId")]
    [InverseProperty("PatientDoctorsDoctor")]
    public virtual Users Doctor { get; set; } = null!;

    [ForeignKey("PatientId")]
    [InverseProperty("PatientDoctorsPatient")]
    public virtual Users Patient { get; set; } = null!;
}
