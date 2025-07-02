namespace GlucoTrack_api.DTOs;

public class UserInfoResponseDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public DateTime? BirthDate { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public string? FiscalCode { get; set; }
    public string? Gender { get; set; }
    public DoctorInfoDto? CurrentDoctor { get; set; }
    public List<RiskFactorDto> RiskFactors { get; set; } = new();
    public List<ComorbidityDto> Comorbidities { get; set; } = new();
    public List<TherapyWithSchedulesResponseDto> Therapies { get; set; } = new();
    public string? Specialization { get; set; }
    public string? AffiliatedHospital { get; set; }
}

public class DoctorInfoDto
{
    public int DoctorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? AffiliatedHospital { get; set; }
}

public class RiskFactorDto
{
    public int RiskFactorId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class TherapyScheduleDto
{
    public int ScheduleId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Frequency { get; set; } = string.Empty;
}
