using GlucoTrack_api.Data;
using GlucoTrack_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


/*
====================================================================================
Sample Data Scenarios (June 2025)
====================================================================================

| UserId | Name              | Role    | Doctor         | Therapy/Medications                | Clinical Scenario Summary                                                               |
|--------|-------------------|---------|----------------|------------------------------------|-----------------------------------------------------------------------------------------|
| 1      | Anna Bianchi      | Doctor  | -              | -                                  | Endocrinologist, follows Marco and Laura                                                |
| 2      | Luca Verdi        | Doctor  | -              | -                                  | Diabetologist, follows Francesco and Giulia                                             |
| 3      | Marco Rossi       | Patient | Dr. Bianchi    | Insulin Rapid + Basal (1/day each) | Perfect adherence: 6 glycemic logs/day (from 2025-06-01 to yesterday), all intakes, no alerts |
| 4      | Laura Giuliani    | Patient | Dr. Bianchi    | Metformin (2/day)                  | 2 days with <6 glycemic logs (2025-06-10, 2025-06-25), 1 missed intake (2025-06-15), 1 critical glycemia, 1 critical symptom |
| 5      | Francesco Bruno   | Patient | Dr. Verdi      | Metformin + Basal (1/day each)     | 3 consecutive days <6 glycemic logs (2025-06-12, 2025-06-13, 2025-06-14), frequent mild hyperglycemia, all intakes correct   |
| 6      | Giulia Neri       | Patient | Dr. Verdi      | Basal (1/day, Therapy A)           | 1 day with no glycemic logs (2025-06-18), 2 missed intakes (2025-06-20, 2025-07-10), 1 severe glycemia, no critical symptoms. |
                                                        | + SGLT2 (1/day, Therapy B)         |

Details:
- All users created on 2025-06-01. All data starts from this date.
- Each patient has an active therapy. Laura has a therapy with a single drug (Metformin) but 2 daily intakes (breakfast and dinner).
- Glycemic logs: 6/day for most days, with exceptions as above (tutte le eccezioni sono a giugno 2025, già passato rispetto a oggi).
- Medication intakes: all correct for Marco e Francesco; Laura e Giulia hanno missed intakes nei giorni indicati sopra.
- Glycemic values: realistic, con giorni/valori che possono generare alert (mild/severe/critical hyperglycemia).
- Symptoms: Laura ha un critical symptom (loss of consciousness) il 2025-06-10.
- Tutte le situazioni sono pensate per triggerare i principali alert di monitoraggio.
- Giulia Neri (UserId=6) è il caso chiave: due terapie attive, ciascuna con un farmaco diverso (Basal insulin e SGLT2 inhibitor), entrambe con un solo daily intake. Questo copre lo scenario di paziente con più terapie anche se ogni terapia ha un solo farmaco e un solo daily intake.
====================================================================================
*/


namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DbController : ControllerBase
    {
        private readonly GlucoTrackDBContext _context;

        public DbController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        [HttpPost("clear")]
        public async Task<IActionResult> Clear()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // L'ordine è importante per rispettare le FK
                await _context.Symptoms.ExecuteDeleteAsync();
                await _context.MedicationIntakes.ExecuteDeleteAsync();
                await _context.GlycemicMeasurements.ExecuteDeleteAsync();
                await _context.PatientRiskFactors.ExecuteDeleteAsync();
                await _context.PatientDoctors.ExecuteDeleteAsync();
                await _context.MedicationSchedules.ExecuteDeleteAsync();
                await _context.Therapies.ExecuteDeleteAsync();
                await _context.AlertRecipients.ExecuteDeleteAsync();
                await _context.Alerts.ExecuteDeleteAsync();
                await _context.ClinicalComorbidities.ExecuteDeleteAsync();
                await _context.ReportedConditions.ExecuteDeleteAsync();
                await _context.Users.ExecuteDeleteAsync();
                await _context.RiskFactors.ExecuteDeleteAsync();
                await _context.MealTypes.ExecuteDeleteAsync();
                await _context.MeasurementTypes.ExecuteDeleteAsync();
                await _context.AlertTypes.ExecuteDeleteAsync();
                await _context.Roles.ExecuteDeleteAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Tutti i dati sono stati eliminati con successo." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("generate-sample-data")]
        public async Task<IActionResult> GenerateSampleData()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Ruoli
                if (!await _context.Roles.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Roles ON");
                    _context.Roles.AddRange(new[] {
                        new Roles { RoleId = 1, RoleName = "Admin" },
                        new Roles { RoleId = 2, RoleName = "Doctor" },
                        new Roles { RoleId = 3, RoleName = "Patient" }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Roles OFF");
                }

                // 2. Utenti
                if (!await _context.Users.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Users ON");
                    _context.Users.AddRange(new[] {
                        new Users { UserId = 1, Username = "drbianchi", PasswordHash = "db", FirstName = "Anna", LastName = "Bianchi", Email = "abianchi@email.com", RoleId = 2, Specialization = "Endocrinology", AffiliatedHospital = "Central Hospital", CreatedAt = DateTime.Parse("2025-06-01") },
                        new Users { UserId = 2, Username = "drverdi", PasswordHash = "dv", FirstName = "Luca", LastName = "Verdi", Email = "lverdi@email.com", RoleId = 2, Specialization = "Diabetology", AffiliatedHospital = "North Hospital", CreatedAt = DateTime.Parse("2025-06-01") },
                        new Users { UserId = 3, Username = "mrossi", PasswordHash = "mr", FirstName = "Marco", LastName = "Rossi", Email = "mrossi@email.com", RoleId = 3, BirthDate = DateOnly.Parse("1990-05-01"), Height = (decimal?)175.5, Weight = (decimal?)80.2, FiscalCode = "RSSMRC90E01H501Y", Gender = "M", CreatedAt = DateTime.Parse("2025-06-01") },
                        new Users { UserId = 4, Username = "lgiuliani", PasswordHash = "lg", FirstName = "Laura", LastName = "Giuliani", Email = "lgiuliani@email.com", RoleId = 3, BirthDate = DateOnly.Parse("1985-09-12"), Height = (decimal?)162.0, Weight = (decimal?)65.0, FiscalCode = "GLNLRA85P52H501Z", Gender = "F", CreatedAt = DateTime.Parse("2025-06-01") },
                        new Users { UserId = 5, Username = "fbruno", PasswordHash = "fb", FirstName = "Francesco", LastName = "Bruno", Email = "fbruno@email.com", RoleId = 3, BirthDate = DateOnly.Parse("1978-03-22"), Height = (decimal?)180.0, Weight = (decimal?)92.0, FiscalCode = "BRNFNC78C22H501T", Gender = "M", CreatedAt = DateTime.Parse("2025-06-01") },
                        new Users { UserId = 6, Username = "gneri", PasswordHash = "gn", FirstName = "Giulia", LastName = "Neri", Email = "gneri@email.com", RoleId = 3, BirthDate = DateOnly.Parse("1992-11-15"), Height = (decimal?)168.0, Weight = (decimal?)70.0, FiscalCode = "NERGLI92S55H501A", Gender = "F", CreatedAt = DateTime.Parse("2025-06-01") },
                        new Users { UserId = 7, Username = "admin1", PasswordHash = "admin", FirstName = "Anna", LastName = "Bianchi", Email = "admin@email.com", RoleId = 1, CreatedAt = DateTime.Parse("2025-06-01") }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Users OFF");
                }

                // 3. Patient-Doctor
                if (!await _context.PatientDoctors.AnyAsync())
                {
                    _context.PatientDoctors.AddRange(new[] {
                        new PatientDoctors { PatientId = 3, DoctorId = 1, StartDate = DateOnly.Parse("2025-06-01") },
                        new PatientDoctors { PatientId = 4, DoctorId = 1, StartDate = DateOnly.Parse("2025-06-01") },
                        new PatientDoctors { PatientId = 5, DoctorId = 2, StartDate = DateOnly.Parse("2025-06-01") },
                        new PatientDoctors { PatientId = 6, DoctorId = 2, StartDate = DateOnly.Parse("2025-06-01") }
                    });
                    await _context.SaveChangesAsync();
                }

                // 4. RiskFactors
                if (!await _context.RiskFactors.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT RiskFactors ON");
                    _context.RiskFactors.AddRange(new[] {
                        new RiskFactors { RiskFactorId = 1, Label = "Smoker", Description = "Active smoker" },
                        new RiskFactors { RiskFactorId = 2, Label = "Ex-smoker", Description = "Quit smoking" },
                        new RiskFactors { RiskFactorId = 3, Label = "Alcohol", Description = "Alcohol dependency issues" },
                        new RiskFactors { RiskFactorId = 4, Label = "Drugs", Description = "Drug dependency issues" },
                        new RiskFactors { RiskFactorId = 5, Label = "Obesity", Description = "High body mass index" }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT RiskFactors OFF");
                }

                // 5. PatientRiskFactors
                if (!await _context.PatientRiskFactors.AnyAsync())
                {
                    _context.PatientRiskFactors.AddRange(new[] {
                        new PatientRiskFactors { UserId = 4, RiskFactorId = 1 },
                        new PatientRiskFactors { UserId = 5, RiskFactorId = 5 },
                        new PatientRiskFactors { UserId = 6, RiskFactorId = 2 }
                    });
                    await _context.SaveChangesAsync();
                }

                // 6. MealTypes
                if (!await _context.MealTypes.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT MealTypes ON");
                    _context.MealTypes.AddRange(new[] {
                        new MealTypes { MealTypeId = 1, Label = "Breakfast", Description = "Morning meal" },
                        new MealTypes { MealTypeId = 2, Label = "Lunch", Description = "Midday meal" },
                        new MealTypes { MealTypeId = 3, Label = "Dinner", Description = "Evening meal" }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT MealTypes OFF");
                }

                // 7. MeasurementTypes
                if (!await _context.MeasurementTypes.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT MeasurementTypes ON");
                    _context.MeasurementTypes.AddRange(new[] {
                        new MeasurementTypes { MeasurementTypeId = 1, Label = "Pre-meal", Description = "Before meal" },
                        new MeasurementTypes { MeasurementTypeId = 2, Label = "Post-meal", Description = "After meal" }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT MeasurementTypes OFF");
                }

                // 8. AlertTypes
                if (!await _context.AlertTypes.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT AlertTypes ON");
                    _context.AlertTypes.AddRange(new[] {
                        new AlertTypes { AlertTypeId = 1, Label = "NO_MEASUREMENTS", Description = "No glycemic measurements registered for the day" },
                        new AlertTypes { AlertTypeId = 2, Label = "PARTIAL_MEASUREMENTS", Description = "Less than 6 glycemic measurements registered for the day" },
                        new AlertTypes { AlertTypeId = 3, Label = "REPEATED_PARTIAL_MEASUREMENTS", Description = "Less than 6 glycemic measurements for 3 consecutive days" },
                        new AlertTypes { AlertTypeId = 4, Label = "MISSED_MEDICATION", Description = "Medication intake not registered" },
                        new AlertTypes { AlertTypeId = 5, Label = "THERAPY_NOT_FOLLOWED", Description = "Therapy not followed for at least 3 days" },
                        new AlertTypes { AlertTypeId = 6, Label = "SLIGHTLY_HIGH_GLUCOSE", Description = "Slightly above average glucose" },
                        new AlertTypes { AlertTypeId = 7, Label = "HIGH_GLUCOSE", Description = "Above average glucose" },
                        new AlertTypes { AlertTypeId = 8, Label = "VERY_HIGH_GLUCOSE", Description = "Well above average glucose" },
                        new AlertTypes { AlertTypeId = 9, Label = "CRITICAL_GLUCOSE", Description = "Critical glycemic value" },
                        new AlertTypes { AlertTypeId = 10, Label = "CRITICAL_SYMPTOM", Description = "Critical symptom reported" },
                        new AlertTypes { AlertTypeId = 11, Label = "NEW_COMORBIDITY", Description = "New comorbidity reported" },
                        new AlertTypes { AlertTypeId = 12, Label = "NEW_CONDITION", Description = "New acute condition reported" }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT AlertTypes OFF");
                }

                // 9. Therapies
                if (!await _context.Therapies.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Therapies ON");
                    _context.Therapies.AddRange(new[] {
                        new Therapies { TherapyId = 1, DoctorId = 1, UserId = 3, Title = "Insulin Rapid + Basal Insulin", Instructions = "Inject 10 UI Rapid before breakfast and 20 UI Basal before dinner", StartDate = DateOnly.Parse("2025-06-01"), CreatedAt = DateTime.Parse("2025-06-01") },
                        new Therapies { TherapyId = 2, DoctorId = 1, UserId = 4, Title = "Metformin", Instructions = "Take 500mg at breakfast and dinner", StartDate = DateOnly.Parse("2025-06-01"), CreatedAt = DateTime.Parse("2025-06-01") },
                        new Therapies { TherapyId = 3, DoctorId = 2, UserId = 5, Title = "Metformin + Basal Insulin", Instructions = "Take 500mg Metformin at breakfast and inject 18 UI Basal before dinner", StartDate = DateOnly.Parse("2025-06-01"), CreatedAt = DateTime.Parse("2025-06-01") },
                        new Therapies { TherapyId = 4, DoctorId = 2, UserId = 6, Title = "Basal Insulin", Instructions = "Inject 16 UI before dinner", StartDate = DateOnly.Parse("2025-06-01"), CreatedAt = DateTime.Parse("2025-06-01") },
                        new Therapies { TherapyId = 5, DoctorId = 2, UserId = 6, Title = "SGLT2 inhibitor", Instructions = "Take 10mg at breakfast", StartDate = DateOnly.Parse("2025-06-01"), CreatedAt = DateTime.Parse("2025-06-01") }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Therapies OFF");
                }

                // 10. MedicationSchedules
                if (!await _context.MedicationSchedules.AnyAsync())
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT MedicationSchedules ON");
                    _context.MedicationSchedules.AddRange(new[] {
                        new MedicationSchedules { MedicationScheduleId = 1, TherapyId = 1, MedicationName = "Rapid insulin", DailyIntakes = 1, Quantity = 10.00m, Unit = "UI" },
                        new MedicationSchedules { MedicationScheduleId = 2, TherapyId = 1, MedicationName = "Basal insulin", DailyIntakes = 1, Quantity = 20.00m, Unit = "UI" },
                        new MedicationSchedules { MedicationScheduleId = 3, TherapyId = 2, MedicationName = "Metformin", DailyIntakes = 2, Quantity = 500.00m, Unit = "mg" },
                        new MedicationSchedules { MedicationScheduleId = 4, TherapyId = 3, MedicationName = "Metformin", DailyIntakes = 1, Quantity = 500.00m, Unit = "mg" },
                        new MedicationSchedules { MedicationScheduleId = 5, TherapyId = 3, MedicationName = "Basal insulin", DailyIntakes = 1, Quantity = 18.00m, Unit = "UI" },
                        new MedicationSchedules { MedicationScheduleId = 6, TherapyId = 4, MedicationName = "Basal insulin", DailyIntakes = 1, Quantity = 16.00m, Unit = "UI" },
                        new MedicationSchedules { MedicationScheduleId = 7, TherapyId = 5, MedicationName = "SGLT2 inhibitor", DailyIntakes = 1, Quantity = 10.00m, Unit = "mg" }
                    });
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT MedicationSchedules OFF");
                }

                // 11. GlycemicMeasurements, MedicationIntakes, Symptoms (scenario-driven)
                // Date range: 2025-05-01 to ieri (oggi-1)
                var startDate = new DateOnly(2025, 5, 1);
                var endDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
                var random = new Random(42); // Seed for reproducibility

                // Helper for glycemia values
                int[] normalGly = { 95, 110, 120, 105, 100, 115 };
                int[] mildHighGly = { 160, 170, 180 };
                int[] highGly = { 220, 240 };
                int[] criticalGly = { 350, 400 };

                // Marco Rossi (UserId=3): 6/day, tutti i giorni, valori normali
                if (!await _context.GlycemicMeasurements.AnyAsync(g => g.UserId == 3))
                {
                    var marcoGly = new List<GlycemicMeasurements>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            marcoGly.Add(new GlycemicMeasurements
                            {
                                UserId = 3,
                                MeasurementDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7 + i * 2))),
                                Value = (short)normalGly[i % normalGly.Length],
                                MeasurementTypeId = (i % 2 == 0) ? 1 : 2,
                                MealTypeId = (i % 3) + 1
                            });
                        }
                    }
                    await _context.GlycemicMeasurements.AddRangeAsync(marcoGly);
                    await _context.SaveChangesAsync();
                }

                // Laura Giuliani (UserId=4): 2 giorni con <6 logs, 1 giorno con critical glycemia
                if (!await _context.GlycemicMeasurements.AnyAsync(g => g.UserId == 4))
                {
                    var lauraGly = new List<GlycemicMeasurements>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        int logs = 6;
                        if (d == new DateOnly(2025, 6, 10) || d == new DateOnly(2025, 6, 25)) logs = 3;
                        for (int i = 0; i < logs; i++)
                        {
                            lauraGly.Add(new GlycemicMeasurements
                            {
                                UserId = 4,
                                MeasurementDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7 + i * 2))),
                                Value = (short)((d == new DateOnly(2025, 6, 10) && i == 0) ? 400 : normalGly[i % normalGly.Length]),
                                MeasurementTypeId = (i % 2 == 0) ? 1 : 2,
                                MealTypeId = (i % 3) + 1
                            });
                        }
                    }
                    await _context.GlycemicMeasurements.AddRangeAsync(lauraGly);
                    await _context.SaveChangesAsync();
                }

                // Francesco Bruno (UserId=5): 3 giorni consecutivi <6 logs, frequenti mild hyperglycemia
                if (!await _context.GlycemicMeasurements.AnyAsync(g => g.UserId == 5))
                {
                    var fraGly = new List<GlycemicMeasurements>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        int logs = 6;
                        if (d >= new DateOnly(2025, 6, 12) && d <= new DateOnly(2025, 6, 14)) logs = 4;
                        for (int i = 0; i < logs; i++)
                        {
                            int val = (i == 0 && d.Day % 3 == 0) ? mildHighGly[random.Next(mildHighGly.Length)] : normalGly[i % normalGly.Length];
                            fraGly.Add(new GlycemicMeasurements
                            {
                                UserId = 5,
                                MeasurementDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7 + i * 2))),
                                Value = (short)val,
                                MeasurementTypeId = (i % 2 == 0) ? 1 : 2,
                                MealTypeId = (i % 3) + 1
                            });
                        }
                    }
                    await _context.GlycemicMeasurements.AddRangeAsync(fraGly);
                    await _context.SaveChangesAsync();
                }

                // Giulia Neri (UserId=6): 1 giorno senza log, 2 giorni con missed intakes, 1 giorno severe glycemia
                if (!await _context.GlycemicMeasurements.AnyAsync(g => g.UserId == 6))
                {
                    var giuliaGly = new List<GlycemicMeasurements>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        if (d == new DateOnly(2025, 6, 18)) continue; // nessun log
                        for (int i = 0; i < 6; i++)
                        {
                            int val = (d == new DateOnly(2025, 7, 5) && i == 2) ? highGly[1] : normalGly[i % normalGly.Length];
                            giuliaGly.Add(new GlycemicMeasurements
                            {
                                UserId = 6,
                                MeasurementDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7 + i * 2))),
                                Value = (short)val,
                                MeasurementTypeId = (i % 2 == 0) ? 1 : 2,
                                MealTypeId = (i % 3) + 1
                            });
                        }
                    }
                    await _context.GlycemicMeasurements.AddRangeAsync(giuliaGly);
                    await _context.SaveChangesAsync();
                }

                // MedicationIntakes: Marco (tutti ok), Laura (1 missed), Francesco (tutti ok), Giulia (2 missed)
                // Marco: TherapyId=1, Schedules 1,2 (Rapid, Basal), 1/day ciascuno
                if (!await _context.MedicationIntakes.AnyAsync(m => m.UserId == 3))
                {
                    var marcoIntakes = new List<MedicationIntakes>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        marcoIntakes.Add(new MedicationIntakes
                        {
                            UserId = 3,
                            MedicationScheduleId = 1,
                            IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7))),
                            ExpectedQuantityValue = 10.00m,
                            Unit = "UI"
                        });
                        marcoIntakes.Add(new MedicationIntakes
                        {
                            UserId = 3,
                            MedicationScheduleId = 2,
                            IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(19))),
                            ExpectedQuantityValue = 20.00m,
                            Unit = "UI"
                        });
                    }
                    await _context.MedicationIntakes.AddRangeAsync(marcoIntakes);
                    await _context.SaveChangesAsync();
                }

                // Laura: TherapyId=2, Schedule 3 (Metformin), 2/day, 1 missed il 2025-06-15 (dinner)
                if (!await _context.MedicationIntakes.AnyAsync(m => m.UserId == 4))
                {
                    var lauraIntakes = new List<MedicationIntakes>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        lauraIntakes.Add(new MedicationIntakes
                        {
                            UserId = 4,
                            MedicationScheduleId = 3,
                            IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7))),
                            ExpectedQuantityValue = 500.00m,
                            Unit = "mg"
                        });
                        if (!(d == new DateOnly(2025, 6, 15))) // missed dinner
                            lauraIntakes.Add(new MedicationIntakes
                            {
                                UserId = 4,
                                MedicationScheduleId = 3,
                                IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(19))),
                                ExpectedQuantityValue = 500.00m,
                                Unit = "mg"
                            });
                    }
                    await _context.MedicationIntakes.AddRangeAsync(lauraIntakes);
                    await _context.SaveChangesAsync();
                }

                // Francesco: TherapyId=3, Schedules 4 (Metformin), 5 (Basal), 1/day ciascuno
                if (!await _context.MedicationIntakes.AnyAsync(m => m.UserId == 5))
                {
                    var fraIntakes = new List<MedicationIntakes>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        fraIntakes.Add(new MedicationIntakes
                        {
                            UserId = 5,
                            MedicationScheduleId = 4,
                            IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7))),
                            ExpectedQuantityValue = 500.00m,
                            Unit = "mg"
                        });
                        fraIntakes.Add(new MedicationIntakes
                        {
                            UserId = 5,
                            MedicationScheduleId = 5,
                            IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(19))),
                            ExpectedQuantityValue = 18.00m,
                            Unit = "UI"
                        });
                    }
                    await _context.MedicationIntakes.AddRangeAsync(fraIntakes);
                    await _context.SaveChangesAsync();
                }

                // Giulia: TherapyId=4 (Basal, sched 6), TherapyId=5 (SGLT2, sched 7), 1/day ciascuno, missed 2025-06-20 (Basal), 2025-07-10 (SGLT2)
                if (!await _context.MedicationIntakes.AnyAsync(m => m.UserId == 6))
                {
                    var giuliaIntakes = new List<MedicationIntakes>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        if (d != new DateOnly(2025, 6, 20))
                            giuliaIntakes.Add(new MedicationIntakes
                            {
                                UserId = 6,
                                MedicationScheduleId = 6,
                                IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(19))),
                                ExpectedQuantityValue = 16.00m,
                                Unit = "UI"
                            });
                        if (d != new DateOnly(2025, 7, 10))
                            giuliaIntakes.Add(new MedicationIntakes
                            {
                                UserId = 6,
                                MedicationScheduleId = 7,
                                IntakeDateTime = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(7))),
                                ExpectedQuantityValue = 10.00m,
                                Unit = "mg"
                            });
                    }
                    await _context.MedicationIntakes.AddRangeAsync(giuliaIntakes);
                    await _context.SaveChangesAsync();
                }

                // Symptoms: almeno 1 ogni 7 giorni per paziente, Laura ha critical symptom il 2025-06-10
                if (!await _context.Symptoms.AnyAsync())
                {
                    var symptomsSeed = new List<Symptoms>();
                    // Marco: lievi, ogni 7 giorni
                    for (var d = startDate; d <= endDate; d = d.AddDays(7))
                        symptomsSeed.Add(new Symptoms
                        {
                            UserId = 3,
                            OccurredAt = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10))),
                            Description = "Mild fatigue"
                        });
                    // Laura: critical il 2025-06-10, altri lievi
                    symptomsSeed.Add(new Symptoms
                    {
                        UserId = 4,
                        OccurredAt = new DateTime(2025, 6, 10, 15, 0, 0),
                        Description = "Loss of consciousness (Critical event)"
                    });
                    for (var d = startDate; d <= endDate; d = d.AddDays(7))
                        if (d != new DateOnly(2025, 6, 10))
                            symptomsSeed.Add(new Symptoms
                            {
                                UserId = 4,
                                OccurredAt = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10))),
                                Description = "Mild headache"
                            });
                    // Francesco: lievi
                    for (var d = startDate; d <= endDate; d = d.AddDays(7))
                        symptomsSeed.Add(new Symptoms
                        {
                            UserId = 5,
                            OccurredAt = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10))),
                            Description = "Mild dizziness"
                        });
                    // Giulia: lievi
                    for (var d = startDate; d <= endDate; d = d.AddDays(7))
                        symptomsSeed.Add(new Symptoms
                        {
                            UserId = 6,
                            OccurredAt = d.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10))),
                            Description = "Mild nausea"
                        });
                    await _context.Symptoms.AddRangeAsync(symptomsSeed);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // --- GENERAZIONE REPORT ---
                var users = await _context.Users.Include(u => u.Role).ToListAsync();
                var doctors = users.Where(u => u.RoleId == 2).ToList();
                var patients = users.Where(u => u.RoleId == 3).ToList();
                var therapies = await _context.Therapies.ToListAsync();
                var schedules = await _context.MedicationSchedules.ToListAsync();
                var patientDoctors = await _context.PatientDoctors.ToListAsync();
                var glyLogs = await _context.GlycemicMeasurements.ToListAsync();
                var medIntakes = await _context.MedicationIntakes.ToListAsync();
                var symptoms = await _context.Symptoms.ToListAsync();

                // --- REPORT IN HTML ---
                var html = "<html><head><meta charset='utf-8'><style>table{border-collapse:collapse;}th,td{border:1px solid #aaa;padding:4px 8px;}th{background:#eee;}</style></head><body>";
                html += $"<h2>Sample Data Report ({DateTime.Now:yyyy-MM-dd HH:mm})</h2>";
                html += "<table>";
                html += "<tr><th>UserId</th><th>Name</th><th>Role</th><th>Doctor</th><th>Therapy/Medications</th><th>Clinical Scenario Summary</th></tr>";
                foreach (var p in patients)
                {
                    var docRel = patientDoctors.FirstOrDefault(pd => pd.PatientId == p.UserId);
                    var doc = docRel != null ? users.FirstOrDefault(u => u.UserId == docRel.DoctorId) : null;
                    var patTherapies = therapies.Where(t => t.UserId == p.UserId).ToList();
                    var therapyDesc = string.Join(" + ", patTherapies.Select(t =>
                        string.Join(", ", schedules.Where(s => s.TherapyId == t.TherapyId).Select(sch => sch.MedicationName + " (" + sch.DailyIntakes + "/day)"))
                    ));
                    // Glycemic logs
                    var logs = glyLogs.Where(g => g.UserId == p.UserId).GroupBy(g => g.MeasurementDateTime.Date).ToList();
                    int totalDays = logs.Count;
                    int daysWith6 = logs.Count(g => g.Count() == 6);
                    int daysWithLess6 = logs.Count(g => g.Count() < 6);
                    int daysNoLogs = 0;
                    if (logs.Count > 0)
                    {
                        var minDate = logs.Min(g => g.Key);
                        var maxDate = logs.Max(g => g.Key);
                        daysNoLogs = Enumerable.Range(0, (maxDate - minDate).Days + 1)
                            .Select(offset => minDate.AddDays(offset))
                            .Count(date => !logs.Any(g => g.Key == date));
                    }
                    // Medication intakes
                    var patIntakes = medIntakes.Where(m => m.UserId == p.UserId).ToList();
                    int missedIntakes = 0;
                    foreach (var t in patTherapies)
                    {
                        var scheds = schedules.Where(s => s.TherapyId == t.TherapyId).ToList();
                        foreach (var s in scheds)
                        {
                            var expected = 0;
                            if (logs.Count > 0)
                            {
                                var minDate = logs.Min(g => g.Key);
                                var maxDate = logs.Max(g => g.Key);
                                expected = ((maxDate - minDate).Days + 1) * s.DailyIntakes;
                            }
                            int actual = patIntakes.Count(i => i.MedicationScheduleId == s.MedicationScheduleId);
                            missedIntakes += Math.Max(0, expected - actual);
                        }
                    }
                    // Glycemic values
                    var critGly = glyLogs.Any(g => g.UserId == p.UserId && g.Value >= 350);
                    var hasHighGly = glyLogs.Any(g => g.UserId == p.UserId && g.Value >= 220 && g.Value < 350);
                    var mildHigh = glyLogs.Any(g => g.UserId == p.UserId && g.Value >= 160 && g.Value < 220);
                    // Symptoms
                    var critSym = symptoms.Any(s => s.UserId == p.UserId && s.Description != null && s.Description.ToLower().Contains("critical"));
                    var summary = "";
                    if (daysNoLogs > 0) summary += $"{daysNoLogs} day(s) with no glycemic logs. ";
                    if (daysWithLess6 > 0) summary += $"{daysWithLess6} day(s) with <6 glycemic logs. ";
                    if (missedIntakes > 0) summary += $"{missedIntakes} missed intake(s). ";
                    if (critGly) summary += "Critical glycemia. ";
                    else if (hasHighGly) summary += "Severe glycemia. ";
                    else if (mildHigh) summary += "Mild hyperglycemia. ";
                    if (critSym) summary += "Critical symptom. ";
                    if (string.IsNullOrWhiteSpace(summary)) summary = "Perfect adherence: 6 glycemic logs/day, all intakes, no alerts.";
                    html += $"<tr><td>{p.UserId}</td><td>{p.FirstName} {p.LastName}</td><td>Patient</td><td>{(doc != null ? doc.FirstName + " " + doc.LastName : "-")}</td><td>{therapyDesc}</td><td>{summary}</td></tr>";
                }
                foreach (var d in doctors)
                {
                    var pats = patientDoctors.Where(pd => pd.DoctorId == d.UserId).Select(pd => users.FirstOrDefault(u => u.UserId == pd.PatientId)).Where(u => u != null).ToList();
                    html += $"<tr><td>{d.UserId}</td><td>{d.FirstName} {d.LastName}</td><td>Doctor</td><td>-</td><td>-</td><td>Follows: {string.Join(", ", pats.Select(p => p.FirstName + " " + p.LastName))}</td></tr>";
                }
                var admins = users.Where(u => u.RoleId == 1).ToList();
                foreach (var a in admins)
                {
                    html += $"<tr><td>{a.UserId}</td><td>{a.FirstName} {a.LastName}</td><td>Admin</td><td>-</td><td>-</td><td>-</td></tr>";
                }
                html += "</table></body></html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
