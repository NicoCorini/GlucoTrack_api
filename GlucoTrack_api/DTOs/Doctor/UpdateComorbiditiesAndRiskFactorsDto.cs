namespace GlucoTrack_api.DTOs
{
    public class UpdateComorbiditiesAndRiskFactorsDto
    {
        public int UserId { get; set; }
        public List<ComorbidityDto> Comorbidities { get; set; } = new();
        public List<int> RiskFactorIds { get; set; } = new();
    }
}
