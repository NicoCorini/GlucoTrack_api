using System;
using System.Linq;
using System.Threading.Tasks;
using GlucoTrack_api.Data;
using GlucoTrack_api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Utils
{
    public static class AlertUtils
    {
        public static async Task<bool> CreateGlycemiaAlertInternal(GlucoTrackDBContext context, int userId, decimal value, DateTime dateTime, string level, string? message = null)

        {
            if (userId <= 0 || value < 40 || value > 400)
                return false;

            // Determina label e destinatari
            string label;
            string msg;
            // Tutti i medici
            var allDoctors = context.Users.Where(u => u.RoleId == 2).Select(u => u.UserId).ToList();
            int? doctorId = context.PatientDoctors.FirstOrDefault(x => x.PatientId == userId)?.DoctorId;
            var recipients = new List<int>();
            if (level == "CRITICAL")
            {
                label = "CRITICAL_GLUCOSE";
                // Tutti i medici
                recipients.AddRange(allDoctors);
                msg = message ?? $"Critical glycemia value: {value} mg/dL at {dateTime:HH:mm} on {dateTime:dd/MM/yyyy}";
            }
            else if (level == "SEVERE")
            {
                label = "VERY_HIGH_GLUCOSE";
                recipients.Add(userId);
                recipients.AddRange(allDoctors);
                msg = message ?? $"Severely high glycemia: {value} mg/dL at {dateTime:HH:mm} on {dateTime:dd/MM/yyyy}";
            }
            else if (level == "MILD")
            {
                label = "HIGH_GLUCOSE";
                recipients.Add(userId);
                recipients.AddRange(allDoctors);
                msg = message ?? $"Moderately high glycemia: {value} mg/dL at {dateTime:HH:mm} on {dateTime:dd/MM/yyyy}";
            }
            else
            {
                return false;
            }

            var alertType = await context.AlertTypes.FirstOrDefaultAsync(a => a.Label == label);
            if (alertType == null) return false;

            var today = dateTime.Date;
            bool exists = await context.Alerts.AnyAsync(a =>
                a.UserId == userId &&
                a.AlertTypeId == alertType.AlertTypeId &&
                a.Message == msg &&
                a.CreatedAt.HasValue &&
                a.CreatedAt.Value.Date == today
            );
            if (exists) return false;

            var alert = new Alerts
            {
                UserId = userId,
                AlertTypeId = alertType.AlertTypeId,
                Message = msg,
                CreatedAt = dateTime
            };
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();

            foreach (var rid in recipients.Distinct())
            {
                if (rid == 0) continue;
                context.AlertRecipients.Add(new AlertRecipients
                {
                    AlertId = alert.AlertId,
                    RecipientUserId = rid,
                    IsRead = false
                });
            }
            await context.SaveChangesAsync();
            return true;
        }
    }
}
