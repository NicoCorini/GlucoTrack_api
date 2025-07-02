using System;
using System.Collections.Generic;

namespace GlucoTrack_api.DTOs
{
    public class AddOrUpdateTherapyRequestDto
    {
        public int? TherapyId { get; set; } // null o <=0 per insert, >0 per update
        public int DoctorId { get; set; }
        public int UserId { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<MedicationScheduleDto> MedicationSchedules { get; set; } = new List<MedicationScheduleDto>();
    }


}
