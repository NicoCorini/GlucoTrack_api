using GlucoTrack_api.Models;

namespace GlucoTrack_api.DTOs;

public class PatientAnalyticsDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }

    // Glicemia: andamento settimanale/mensile (media, min, max, stddev per periodo)
    public List<GlycemicTrendDto> GlycemicTrends { get; set; } = new();
    // Statistiche ultime 4 settimane (min, max, stddev)
    public GlycemicStatsDto GlycemicLast4WeeksStats { get; set; } = new();
    // Distribuzione valori glicemici (istogramma, boxplot)
    public GlycemicDistributionDto GlycemicDistribution { get; set; } = new();
    // Adesione terapia: % assunzioni programmate vs effettive
    public TherapyAdherenceDto TherapyAdherence { get; set; } = new();
    // Sintomi recenti
    public List<SymptomDto> RecentSymptoms { get; set; } = new();
    // Alert clinici recenti
    public List<AlertDto> RecentAlerts { get; set; } = new();
    // Comorbidità e fattori di rischio
    public List<ComorbidityDto> Comorbidities { get; set; } = new();
    public List<RiskFactors> RiskFactors { get; set; } = new();
    // Assunzioni farmaci extra-terapia recenti
    public List<ExtraMedicationIntakeDto> RecentExtraMedicationIntakes { get; set; } = new();
}

public class GlycemicTrendDto
{
    public string Period { get; set; } = string.Empty; // es: "2025-W27" o "2025-07"
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double StdDev { get; set; }
}

public class GlycemicDistributionDto
{
    public List<int> Values { get; set; } = new();
    public double Q1 { get; set; }
    public double Median { get; set; }
    public double Q3 { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public List<int> Outliers { get; set; } = new();
    public int TargetMin { get; set; }
    public int TargetMax { get; set; }
}

public class TherapyAdherenceDto
{
    public int ScheduledIntakes { get; set; }
    public int PerformedIntakes { get; set; }
    public double AdherencePercent { get; set; }
}

public class SymptomDto
{
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public class ExtraMedicationIntakeDto
{
    public string MedicationName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime IntakeDateTime { get; set; }
    public string? Note { get; set; }
}