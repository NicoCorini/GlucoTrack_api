using System;

namespace GlucoTrack_api.DTOs
{
    public class MedicationScheduleDto
    {
        public int? MedicationScheduleId { get; set; } // null o <=0 per insert, >0 per update
        public string MedicationName { get; set; } = string.Empty;
        public decimal ExpectedQuantity { get; set; }
        public string ExpectedUnit { get; set; } = string.Empty;
        public TimeOnly ScheduledTime { get; set; }
    }
}
