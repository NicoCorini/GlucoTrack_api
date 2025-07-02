namespace GlucoTrack_api.DTOs
{
    public class AddMedicationLogRequestDto
    {
        public int UserId { get; set; }
        public DateTime IntakeDateTime { get; set; }
        public double ExpectedQuantityValue { get; set; }
        public string? Unit { get; set; }
        public string? Note { get; set; }
        public string? MedicationTakenName { get; set; }
        public int? MedicationScheduleId { get; set; }
    }
}
