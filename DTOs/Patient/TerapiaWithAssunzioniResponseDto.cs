namespace GlucoTrack_api.DTOs.Patient
{
    public class TherapyWithSchedulesResponseDto
    {
        public int TherapyId { get; set; }
        public string? Instructions { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<MedicationScheduleDto> MedicationSchedules { get; set; } = new();
    }

    public class MedicationScheduleDto
    {
        public string? MedicationName { get; set; }
        public double ExpectedQuantity { get; set; }
        public string? ExpectedUnit { get; set; }
        public DateTime ScheduledDateTime { get; set; }
    }
}
