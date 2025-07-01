using System.ComponentModel.DataAnnotations;

namespace GlucoTrack_api.DTOs.Patient;

public class PatientInfoRequestDto
{
    [Required]
    public int UserId { get; set; }
}
