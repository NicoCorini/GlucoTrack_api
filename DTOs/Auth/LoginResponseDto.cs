using System.ComponentModel.DataAnnotations;

namespace GlucoTrack_api.DTOs.Auth;

public class LoginResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime LastAccess { get; set; }
    public int RoleId { get; set; }
}
