namespace GlucoTrack_api.DTOs;

public class GlycemiaAlertRequest
{
    public int UserId { get; set; }
    public decimal Value { get; set; }
    public DateTime DateTime { get; set; }
    public string Level { get; set; } = string.Empty; // "MILD", "SEVERE", "CRITICAL"
    public string? Message { get; set; }
}