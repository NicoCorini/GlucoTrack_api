using System.ComponentModel.DataAnnotations;

namespace GlucoTrack_api.DTOs;

public class LoginRequestDto
{
    [Required]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
