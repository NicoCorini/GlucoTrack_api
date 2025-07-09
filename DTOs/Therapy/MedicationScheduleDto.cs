using System;

namespace GlucoTrack_api.DTOs
{
    public class MedicationScheduleDto
    {
        public int? MedicationScheduleId { get; set; } // null o <=0 per insert, >0 per update
        public string MedicationName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int DailyIntakes { get; set; } // Numero di assunzioni giornaliere
    }
}
