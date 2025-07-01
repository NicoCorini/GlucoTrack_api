using System;

namespace GlucoTrack_api.DTOs.Patient
{
    public class AddSymptomLogRequestDto
    {
        public int UserId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
