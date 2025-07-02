using System;

namespace GlucoTrack_api.DTOs.Patient
{
    public class AddReportedConditionRequestDto
    {
        public int? ConditionId { get; set; } // Null for insert, set for update
        public int UserId { get; set; }
        public required string Description { get; set; } // Name/description of the reported condition
        public DateTime? StartDate { get; set; } // When the condition was reported/diagnosed
        public DateTime? EndDate { get; set; } // Null if ongoing, otherwise when resolved
    }
}
