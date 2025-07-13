using System;
using System.Collections.Generic;

namespace GlucoTrack_api.DTOs
{
    public class AddOrUpdateTherapyRequestDto
    {
        public int? TherapyId { get; set; } // null o <=0 per insert, >0 per update
        public int DoctorId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public List<MedicationScheduleDto> MedicationSchedules { get; set; } = new List<MedicationScheduleDto>();
    }


}
