namespace GlucoTrack_api.DTOs
{
    public class AddGlycemicLogRequestDto
    {
        public int GlycemicMeasurementId { get; set; }
        public int UserId { get; set; }
        public DateTime MeasurementDateTime { get; set; }
        public int Value { get; set; }
        public string? Note { get; set; }
        public int MeasurementTypeId { get; set; }
        public int MealTypeId { get; set; }
    }
}
