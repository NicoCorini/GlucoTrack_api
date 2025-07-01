namespace GlucoTrack_api.DTOs.Patient;

public class ComorbidityDto
{
    public string Comorbidity { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
