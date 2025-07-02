namespace GlucoTrack_api.DTOs
{
    public class AddSymptomLogRequestDto
    {
        public int SymptomId { get; set; } = 0; // 0 = insert, >0 = update
        public int UserId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
