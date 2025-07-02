namespace GlucoTrack_api.DTOs;

public class ComorbidityDto
{
    public string Comorbidity { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
