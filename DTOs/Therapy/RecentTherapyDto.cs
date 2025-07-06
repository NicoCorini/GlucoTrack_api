using System;
using System.Collections.Generic;

namespace GlucoTrack_api.DTOs
{
    public class RecentTherapyDto
    {
        public int TherapyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int DoctorId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RecentMedicationScheduleDto> MedicationSchedules { get; set; } = new();
    }

    public class RecentMedicationScheduleDto
    {
        public int MedicationScheduleId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public double ExpectedQuantity { get; set; }
        public string ExpectedUnit { get; set; } = string.Empty;
        public string ScheduledTime { get; set; } = string.Empty;
    }
}
