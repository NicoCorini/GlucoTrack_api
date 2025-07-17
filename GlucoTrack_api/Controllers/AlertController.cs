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
        /// Returns all unresolved alerts received by a user (as recipient), including type, message, status, and other details.
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
        /// Returns all alerts received by a user (as recipient), including type, message, status, and other details.
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
        /// Marks an alert as resolved for a specific recipient (by AlertRecipientId).
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

            // Mark as read and resolved
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
        /// Manually creates a glycemia alert (e.g., from frontend) for an out-of-range value.
        /// </summary>
        [HttpPost("create-glycemia-alert")]
        public async Task<IActionResult> CreateGlycemiaAlert([FromBody] GlycemiaAlertRequest req)
        {
            if (req == null || req.UserId <= 0)
                return BadRequest("Invalid data");

            // Determine label and recipients
            string label;
            int? doctorId = _context.PatientDoctors.FirstOrDefault(x => x.PatientId == req.UserId)?.DoctorId;
            int[] recipients;
            string msg;
            // Map frontend levels to existing AlertTypes labels
            if (req.Level == "CRITICAL")
            {
                label = "CRITICAL_GLUCOSE";
                if (doctorId == null) return BadRequest("No doctor found for patient");
                recipients = new[] { req.UserId, doctorId.Value };
                msg = req.Message ?? $"Critical glycemia value: {req.Value} mg/dL at {req.DateTime:HH:mm} on {req.DateTime:dd/MM/yyyy}";
            }
            else if (req.Level == "SEVERE")
            {
                label = "VERY_HIGH_GLUCOSE";
                if (doctorId != null)
                    recipients = new[] { req.UserId, doctorId.Value };
                else
                    recipients = new[] { req.UserId };
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

            // Create alert (logic similar to MonitoringUtils)
            var alertType = await _context.AlertTypes.FirstOrDefaultAsync(a => a.Label == label);
            if (alertType == null) return BadRequest("Alert type not found");

            var today = DateTime.Today;
            bool exists = await _context.Alerts.AnyAsync(a =>
                a.UserId == req.UserId &&
                a.AlertTypeId == alertType.AlertTypeId &&
                a.Message == msg &&
                a.CreatedAt.Date == today
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
