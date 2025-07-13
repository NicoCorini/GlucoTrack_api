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

        /// <summary>
        /// Creates a glycemia alert for a user, based on the provided value and severity level. Determines the alert type, message, and recipients, and saves the alert and its recipients to the database.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="userId">The ID of the user for whom the alert is generated.</param>
        /// <param name="value">The glycemia value that triggered the alert.</param>
        /// <param name="dateTime">The date and time of the measurement.</param>
        /// <param name="level">The severity level: CRITICAL, SEVERE, or MILD.</param>
        /// <param name="message">Optional custom message for the alert.</param>
        /// <returns>True if the alert was created, false otherwise.</returns>
        public static async Task<bool> CreateGlycemiaAlertInternal(GlucoTrackDBContext context, int userId, decimal value, DateTime dateTime, string level, string? message = null)
        {
            if (userId <= 0 || value < 40 || value > 400)
                return false;

            // Determine label and recipients
            string label;
            string msg;
            // All doctors
            var allDoctors = context.Users.Where(u => u.RoleId == 2).Select(u => u.UserId).ToList();
            int? doctorId = context.PatientDoctors.FirstOrDefault(x => x.PatientId == userId)?.DoctorId;
            var recipients = new List<int>();
            if (level == "CRITICAL")
            {
                label = "CRITICAL_GLUCOSE";
                // All doctors
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
                a.CreatedAt.Date == today
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
