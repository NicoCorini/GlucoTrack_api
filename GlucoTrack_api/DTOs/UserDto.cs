using System;

namespace GlucoTrack_api.DTOs
{
    /// <summary>
    /// DTO for user creation and update (AdminController).
    /// Contains only primitive fields, no navigation properties.
    /// </summary>
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public DateOnly? BirthDate { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string? FiscalCode { get; set; }
        public string? Gender { get; set; }
        public string? Specialization { get; set; }
        public string? AffiliatedHospital { get; set; }
    }
}
