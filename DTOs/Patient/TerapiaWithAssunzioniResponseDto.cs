namespace GlucoTrack_api.DTOs
{
    public class TherapyWithSchedulesResponseDto
    {
        public int TherapyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<MedicationScheduleDto> MedicationSchedules { get; set; } = new();
    }

}
