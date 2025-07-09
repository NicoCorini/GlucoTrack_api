namespace GlucoTrack_api.DTOs
{
    public class AlertDto
    {
        public int AlertRecipientId { get; set; }
        public int AlertId { get; set; }
        public int RecipientUserId { get; set; }
        // Utente a cui si riferisce l'evento dell'alert (es. paziente)
        public int UserId { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? NotifiedAt { get; set; }
        // Info alert
        public string Message { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly? ReferenceDate { get; set; }
        public string? ReferencePeriod { get; set; }
        public int? ReferenceObjectId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        // Tipo alert
        public int AlertTypeId { get; set; }
        public string AlertTypeLabel { get; set; } = string.Empty;
        public string? AlertTypeDescription { get; set; }

        // Info utente soggetto dell'alert (es. paziente)
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
    }
}
