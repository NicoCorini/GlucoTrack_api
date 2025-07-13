using System;
using System.Collections.Generic;

namespace GlucoTrack_api.DTOs
{
    public class DoctorDashboardSummaryDto
    {
        public List<DashboardPatientSummaryDto> InLinePatients { get; set; } = new();
        public List<DashboardPatientSummaryDto> HighAvgGlucosePatients { get; set; } = new();
        public DashboardAlertSummaryDto GlycemiaAlerts { get; set; } = new();
    }

    public class DashboardPatientSummaryDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public double WeeklyAvgGlycemia { get; set; }
        public string? Trend { get; set; } // up, down, stable
    }

    public class DashboardAlertSummaryDto
    {
        public int TotalOpen { get; set; }
        public int CriticalCount { get; set; }
        public int SevereCount { get; set; }
        public int MildCount { get; set; }
        public List<DashboardAlertDetailDto> Alerts { get; set; } = new();
    }

    public class DashboardAlertDetailDto
    {
        public int AlertRecipientId { get; set; }
        public int AlertId { get; set; }
        public int PatientId { get; set; }
        public string PatientFirstName { get; set; } = string.Empty;
        public string PatientLastName { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty; // CRITICAL, SEVERE, MILD
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
