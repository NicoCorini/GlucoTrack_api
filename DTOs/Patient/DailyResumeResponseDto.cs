using GlucoTrack_api.Models;

namespace GlucoTrack_api.DTOs.Patient
{
    public class DailyResumeResponseDto
    {
        public List<GlycemicMeasurements> GlycemicMeasurements { get; set; } = new();
        public List<MedicationIntakes> MedicationIntakes { get; set; } = new();
        public List<Symptoms> Symptoms { get; set; } = new();
        public List<ReportedConditions> ReportedConditions { get; set; } = new();
    }


}
