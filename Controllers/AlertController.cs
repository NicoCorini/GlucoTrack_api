using GlucoTrack_api.Models;
using GlucoTrack_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlucoTrack_api.DTOs;


namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AlertController : Controller
    {
        private readonly GlucoTrackDBContext _context;

        public AlertController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Restituisce tutti gli alert non risolti ricevuti da un utente (come destinatario), con dettagli tipo, messaggio, stato, ecc.
        /// </summary>
        [HttpGet("user-not-resolved-alerts")]
        public async Task<IActionResult> GetUserNotResolvedAlerts([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid userId.");


            var alerts = await _context.AlertRecipients
                .Where(ar => ar.RecipientUserId == userId && ar.Alert.Status != "resolved")
                .Include(ar => ar.Alert)
                    .ThenInclude(a => a.AlertType)
                .Include(ar => ar.Alert.User)
                .OrderByDescending(ar => ar.Alert.CreatedAt)
                .Select(ar => new DTOs.AlertDto
                {
                    AlertRecipientId = ar.AlertRecipientId,
                    AlertId = ar.AlertId,
                    RecipientUserId = ar.RecipientUserId,
                    UserId = ar.Alert.UserId,
                    IsRead = ar.IsRead,
                    ReadAt = ar.ReadAt,
                    NotifiedAt = ar.NotifiedAt,
                    Message = ar.Alert.Message ?? string.Empty,
                    CreatedAt = ar.Alert.CreatedAt,
                    Status = ar.Alert.Status ?? string.Empty,
                    ReferenceDate = ar.Alert.ReferenceDate,
                    ReferencePeriod = ar.Alert.ReferencePeriod,
                    ReferenceObjectId = ar.Alert.ReferenceObjectId,
                    ResolvedAt = ar.Alert.ResolvedAt,
                    AlertTypeId = ar.Alert.AlertTypeId,
                    AlertTypeLabel = ar.Alert.AlertType.Label ?? string.Empty,
                    AlertTypeDescription = ar.Alert.AlertType.Description,
                    UserFirstName = ar.Alert.User.FirstName,
                    UserLastName = ar.Alert.User.LastName
                })
                .ToListAsync();

            return Ok(alerts);
        }

        /// <summary>
        /// Restituisce tutti gli alert non risolti ricevuti da un utente (come destinatario), con dettagli tipo, messaggio, stato, ecc.
        /// </summary>
        [HttpGet("user-alerts")]
        public async Task<IActionResult> GetUserAlerts([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid userId.");


            var alerts = await _context.AlertRecipients
                .Where(ar => ar.RecipientUserId == userId)
                .Include(ar => ar.Alert)
                    .ThenInclude(a => a.AlertType)
                .Include(ar => ar.Alert.User)
                .OrderByDescending(ar => ar.Alert.CreatedAt)
                .Select(ar => new DTOs.AlertDto
                {
                    AlertRecipientId = ar.AlertRecipientId,
                    AlertId = ar.AlertId,
                    RecipientUserId = ar.RecipientUserId,
                    UserId = ar.Alert.UserId,
                    IsRead = ar.IsRead,
                    ReadAt = ar.ReadAt,
                    NotifiedAt = ar.NotifiedAt,
                    Message = ar.Alert.Message ?? string.Empty,
                    CreatedAt = ar.Alert.CreatedAt,
                    Status = ar.Alert.Status ?? string.Empty,
                    ReferenceDate = ar.Alert.ReferenceDate,
                    ReferencePeriod = ar.Alert.ReferencePeriod,
                    ReferenceObjectId = ar.Alert.ReferenceObjectId,
                    ResolvedAt = ar.Alert.ResolvedAt,
                    AlertTypeId = ar.Alert.AlertTypeId,
                    AlertTypeLabel = ar.Alert.AlertType.Label ?? string.Empty,
                    AlertTypeDescription = ar.Alert.AlertType.Description,
                    UserFirstName = ar.Alert.User.FirstName,
                    UserLastName = ar.Alert.User.LastName
                })
                .ToListAsync();

            return Ok(alerts);
        }

        /// <summary>
        /// Segna come risolto un alert per un destinatario specifico (AlertRecipientId).
        /// </summary>
        [HttpPost("resolve-alert")]
        public async Task<IActionResult> ResolveAlert([FromQuery] int alertRecipientId)
        {
            if (alertRecipientId <= 0)
                return BadRequest("Invalid alertRecipientId.");

            var recipient = await _context.AlertRecipients
                .Include(ar => ar.Alert)
                .FirstOrDefaultAsync(ar => ar.AlertRecipientId == alertRecipientId);

            if (recipient == null)
                return NotFound("AlertRecipient not found.");

            // Segna come letto e risolto
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;
            if (recipient.Alert != null)
            {
                recipient.Alert.Status = "resolved";
                recipient.Alert.ResolvedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            return Ok("Alert resolved for recipient.");
        }


        /// <summary>
        /// Crea un alert glicemico manualmente (es. da frontend) per un valore fuori soglia.
        /// </summary>
        [HttpPost("create-glycemia-alert")]
        public async Task<IActionResult> CreateGlycemiaAlert([FromBody] GlycemiaAlertRequest req)
        {
            if (req == null || req.UserId <= 0 || req.Value < 40 || req.Value > 400)
                return BadRequest("Invalid data");

            // Determina label e destinatari
            string label;
            int? doctorId = _context.PatientDoctors.FirstOrDefault(x => x.PatientId == req.UserId)?.DoctorId;
            int[] recipients;
            string msg;
            // Mappa i livelli frontend su label AlertTypes esistenti
            if (req.Level == "CRITICAL")
            {
                label = "CRITICAL_GLUCOSE";
                if (doctorId == null) return BadRequest("No doctor found for patient");
                recipients = new[] { doctorId.Value };
                msg = req.Message ?? $"Critical glycemia value: {req.Value} mg/dL at {req.DateTime:HH:mm} on {req.DateTime:dd/MM/yyyy}";
            }
            else if (req.Level == "SEVERE")
            {
                label = "VERY_HIGH_GLUCOSE";
                recipients = doctorId != null ? new[] { req.UserId, doctorId.Value } : new[] { req.UserId };
                msg = req.Message ?? $"Severely high glycemia: {req.Value} mg/dL at {req.DateTime:HH:mm} on {req.DateTime:dd/MM/yyyy}";
            }
            else if (req.Level == "MILD")
            {
                label = "HIGH_GLUCOSE";
                recipients = new[] { req.UserId };
                msg = req.Message ?? $"Moderately high glycemia: {req.Value} mg/dL at {req.DateTime:HH:mm} on {req.DateTime:dd/MM/yyyy}";
            }
            else
            {
                return BadRequest("Invalid level");
            }

            // Crea alert (logica simile a MonitoringUtils)
            var alertType = await _context.AlertTypes.FirstOrDefaultAsync(a => a.Label == label);
            if (alertType == null) return BadRequest("Alert type not found");

            var today = DateTime.Today;
            bool exists = await _context.Alerts.AnyAsync(a =>
                a.UserId == req.UserId &&
                a.AlertTypeId == alertType.AlertTypeId &&
                a.Message == msg &&
                a.CreatedAt.HasValue &&
                a.CreatedAt.Value.Date == today
            );
            if (exists) return Ok(new { created = false, reason = "Duplicate" });

            var alert = new Models.Alerts
            {
                UserId = req.UserId,
                AlertTypeId = alertType.AlertTypeId,
                Message = msg,
                CreatedAt = DateTime.Now
            };
            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            foreach (var rid in recipients.Distinct())
            {
                if (rid == 0) continue;
                _context.AlertRecipients.Add(new Models.AlertRecipients
                {
                    AlertId = alert.AlertId,
                    RecipientUserId = rid,
                    IsRead = false
                });
            }
            await _context.SaveChangesAsync();

            return Ok(new { created = true, alertId = alert.AlertId });
        }
    }
}
