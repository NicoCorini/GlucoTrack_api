using GlucoTrack_api.Models;
using GlucoTrack_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlucoTrack_api.DTOs;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DoctorController : Controller
    {
        private readonly GlucoTrackDBContext _context;

        public DoctorController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public IActionResult GetDoctorDashboard([FromQuery] int doctorId)
        {
            return Ok("TBD");
        }

        [HttpGet("recent-therapies")]
        public async Task<IActionResult> GetDoctorRecentTherapies([FromQuery] int doctorId)
        {

            // Solo terapie attive (EndDate == null)
            var recentTherapiesRaw = await _context.Therapies
                .Where(t => t.DoctorId == doctorId && t.EndDate == null)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentTherapies = recentTherapiesRaw.Select(t => new RecentTherapyDto
            {
                TherapyId = t.TherapyId,
                Title = t.Title ?? string.Empty,
                Instructions = t.Instructions ?? string.Empty,
                StartDate = t.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = t.EndDate,
                DoctorId = t.DoctorId,
                UserId = t.UserId,
                CreatedAt = t.CreatedAt ?? DateTime.MinValue,
                MedicationSchedules = _context.MedicationSchedules
                    .Where(ms => ms.TherapyId == t.TherapyId)
                    .Select(ms => new RecentMedicationScheduleDto
                    {
                        MedicationScheduleId = ms.MedicationScheduleId,
                        MedicationName = ms.MedicationName ?? string.Empty,
                        ExpectedQuantity = (double)ms.ExpectedQuantity,
                        ExpectedUnit = ms.ExpectedUnit ?? string.Empty,
                        ScheduledTime = ms.ScheduledTime.ToString("HH:mm")
                    })
                    .ToList()
            }).ToList();

            if (recentTherapies == null || !recentTherapies.Any())
                return NotFound("No recent therapies found for this doctor.");

            return Ok(recentTherapies);
        }

        [HttpGet("patients")]
        public async Task<ActionResult<List<Users>>> GetDoctorPatients(
            [FromQuery] int doctorId,
            [FromQuery] int page = 0,
            [FromQuery] string search = "",
            [FromQuery] bool onlyDoctorPatients = false,
            [FromQuery] int minAge = 0,
            [FromQuery] int maxAge = 120,
            [FromQuery] string gender = "",
            [FromQuery] string patientStatus = "")
        {
            if (page < 0 || minAge < 0 || maxAge < 0 || minAge > maxAge)
                return BadRequest("Invalid parameters.");

            IQueryable<Users> users = _context.Users;

            // Filter for doctor's patients (many-to-many relationship)
            if (onlyDoctorPatients)
            {
                users = users.Where(u =>
                    _context.PatientDoctors.Any(pd =>
                        pd.DoctorId == doctorId &&
                        pd.PatientId == u.UserId &&
                        (pd.EndDate == null || pd.EndDate > DateOnly.FromDateTime(DateTime.UtcNow))
                    )
                );
            }

            // Text search
            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    (u.FirstName != null && u.FirstName.Contains(search)) ||
                    (u.LastName != null && u.LastName.Contains(search)) ||
                    (u.Email != null && u.Email.Contains(search)));
            }

            // Gender filter
            if (!string.IsNullOrEmpty(gender))
            {
                users = users.Where(u => u.Gender == gender);
            }

            // Age filter
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly minDate = today.AddYears(-maxAge);
            DateOnly maxDate = today.AddYears(-minAge);
            users = users.Where(u => u.BirthDate != null && u.BirthDate >= minDate && u.BirthDate <= maxDate);

            // Pagination
            var result = await users
                .Select(u => new Users
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    BirthDate = u.BirthDate ?? today,
                    Gender = u.Gender ?? string.Empty,
                    // ...altre proprietà se necessario...
                })
                .Skip(page * 10)
                .Take(10)
                .ToListAsync();

            if (!result.Any())
                return NotFound("No patients found matching the criteria.");

            return Ok(result);
        }

        public class TherapyWithSchedules
        {
            public Therapies Therapy { get; set; } = new Therapies();
            public List<MedicationSchedules> MedicationSchedules { get; set; } = new List<MedicationSchedules>();
        }

        [HttpPost("therapy")]
        public async Task<IActionResult> AddOrUpdateTherapy([FromBody] AddOrUpdateTherapyRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid therapy data.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool isUpdate = dto.TherapyId.HasValue && dto.TherapyId.Value > 0;
                if (isUpdate)
                {
                    // Recupera la terapia originale
                    var oldTherapy = await _context.Therapies.FirstOrDefaultAsync(t => t.TherapyId == dto.TherapyId);
                    if (oldTherapy == null)
                        return NotFound("Therapy not found.");

                    // Chiudi la terapia attuale (endDate = oggi)
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);

                    if (oldTherapy.StartDate > today)
                    {
                        // TODO: hard delete della terapia vuota, prima elimino scheduled intakes

                        var medicationSchedules = await _context.MedicationSchedules
                            .Where(ms => ms.TherapyId == oldTherapy.TherapyId)
                            .ToListAsync();

                        _context.MedicationSchedules.RemoveRange(medicationSchedules);
                        await _context.SaveChangesAsync();

                        _context.Therapies.Remove(oldTherapy);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        oldTherapy.EndDate = today;
                        _context.Therapies.Update(oldTherapy);
                        await _context.SaveChangesAsync();
                    }

                    // Crea la nuova terapia da domani, collegata alla precedente
                    var tomorrow = today.AddDays(1);
                    var newTherapy = new Therapies
                    {
                        DoctorId = dto.DoctorId,
                        UserId = dto.UserId,
                        Title = dto.Title,
                        Instructions = dto.Instructions,
                        StartDate = tomorrow,
                        EndDate = null,
                        PreviousTherapyId = oldTherapy.StartDate > today ? null : oldTherapy.TherapyId,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.Therapies.Add(newTherapy);
                    await _context.SaveChangesAsync();

                    // Inserisci le medication schedules per la nuova terapia
                    foreach (var msDto in dto.MedicationSchedules)
                    {
                        var ms = new MedicationSchedules
                        {
                            TherapyId = newTherapy.TherapyId,
                            MedicationName = msDto.MedicationName,
                            ExpectedQuantity = msDto.ExpectedQuantity,
                            ExpectedUnit = msDto.ExpectedUnit,
                            ScheduledTime = msDto.ScheduledTime
                        };
                        _context.MedicationSchedules.Add(ms);
                    }
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Nuova terapia: parte da domani, nessuna data fine, nessun collegamento precedente
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var tomorrow = today.AddDays(1);
                    var therapyToAdd = new Therapies
                    {
                        DoctorId = dto.DoctorId,
                        UserId = dto.UserId,
                        Title = dto.Title,
                        Instructions = dto.Instructions,
                        StartDate = tomorrow,
                        EndDate = null,
                        PreviousTherapyId = null,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.Therapies.Add(therapyToAdd);
                    await _context.SaveChangesAsync();

                    foreach (var msDto in dto.MedicationSchedules)
                    {
                        var ms = new MedicationSchedules
                        {
                            TherapyId = therapyToAdd.TherapyId,
                            MedicationName = msDto.MedicationName,
                            ExpectedQuantity = msDto.ExpectedQuantity,
                            ExpectedUnit = msDto.ExpectedUnit,
                            ScheduledTime = msDto.ScheduledTime
                        };
                        _context.MedicationSchedules.Add(ms);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Ok("Therapy saved successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("therapy")]
        public async Task<IActionResult> GetTherapy([FromQuery] int therapyId)
        {
            if (therapyId <= 0)
                return BadRequest("Invalid therapy ID.");

            var therapy = await _context.Therapies
                .Where(t => t.TherapyId == therapyId)
                .Select(t => new
                {
                    t.TherapyId,
                    t.Title,
                    t.Instructions,
                    t.StartDate,
                    t.EndDate,
                    t.DoctorId,
                    t.UserId,
                    MedicationSchedules = _context.MedicationSchedules
                        .Where(ms => ms.TherapyId == t.TherapyId)
                        .Select(ms => new
                        {
                            ms.MedicationName,
                            ms.ExpectedQuantity,
                            ms.ExpectedUnit,
                            ms.ScheduledTime
                        })
                        .ToList(),
                    Patient = _context.Users
                        .Where(u => u.UserId == t.UserId)
                        .Select(u => new
                        {
                            u.UserId,
                            u.FirstName,
                            u.LastName,
                            u.Email
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (therapy == null)
                return NotFound("Therapy not found.");

            return Ok(therapy);
        }

        [HttpDelete("therapy")]
        public async Task<IActionResult> SoftDeleteTherapy([FromQuery] int therapyId)
        {
            if (therapyId <= 0)
                return BadRequest("Invalid therapy ID.");

            var therapy = await _context.Therapies
                .FirstOrDefaultAsync(t => t.TherapyId == therapyId);

            if (therapy == null)
                return NotFound("Therapy not found.");

            try
            {
                // Imposta la data di fine a oggi (soft delete, non elimina dal DB)
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                therapy.EndDate = today;
                _context.Therapies.Update(therapy);
                await _context.SaveChangesAsync();
                return Ok("Therapy end date set to today (soft delete). Therapy remains in history.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("hard-delete-therapy")]
        public async Task<IActionResult> HardDeleteTherapy([FromQuery] int therapyId)
        {
            if (therapyId <= 0)
                return BadRequest("Invalid therapy ID.");

            var therapy = await _context.Therapies
                .FirstOrDefaultAsync(t => t.TherapyId == therapyId);
            if (therapy == null)
                return NotFound("Therapy not found.");

            try
            {
                // Elimina la terapia e le relative medication schedules
                _context.Therapies.Remove(therapy);
                var medicationSchedules = await _context.MedicationSchedules
                    .Where(ms => ms.TherapyId == therapyId)
                    .ToListAsync();
                _context.MedicationSchedules.RemoveRange(medicationSchedules);
                await _context.SaveChangesAsync();
                return Ok("Therapy and related medication schedules deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("patient-analytics")]
        public async Task<IActionResult> GetPatientAnalytics([FromQuery] int userId)
        {
            // Recupera info base paziente
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound("Patient not found.");

            var dto = new PatientAnalyticsDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                BirthDate = user.BirthDate.HasValue ? user.BirthDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                Gender = user.Gender,
                Height = user.Height,
                Weight = user.Weight,
            };


            // Glicemia: andamento settimanale/mensile (media, min, max, stddev per periodo)
            var now = DateTime.UtcNow;
            var fromDate = now.AddMonths(-6); // ultimi 6 mesi
            var glycemic = await _context.GlycemicMeasurements
                .Where(g => g.UserId == userId && g.MeasurementDateTime >= fromDate)
                .ToListAsync();

            // Raggruppa per settimana (ultime 4 settimane)
            var last4WeeksMonday = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday).AddDays(-21); // Lunedì di 4 settimane fa
            // Costruisci le ultime 4 settimane (anche se vuote)
            var weeklyTrends = new List<DTOs.GlycemicTrendDto>();
            for (int i = 3; i >= 0; i--)
            {
                var weekStart = last4WeeksMonday.AddDays(i * 7);
                var weekEnd = weekStart.AddDays(7);
                var weekYear = System.Globalization.ISOWeek.GetYear(weekStart);
                var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(weekStart);
                var periodKey = $"{weekYear}-W{weekNum:D2}";
                var weekData = glycemic.Where(g => g.MeasurementDateTime.Date >= weekStart && g.MeasurementDateTime.Date < weekEnd).ToList();
                if (weekData.Any())
                {
                    double avg = weekData.Average(x => x.Value);
                    weeklyTrends.Add(new DTOs.GlycemicTrendDto
                    {
                        Period = periodKey,
                        Average = avg,
                        Min = weekData.Min(x => x.Value),
                        Max = weekData.Max(x => x.Value),
                        StdDev = Math.Sqrt(weekData.Select(x => Math.Pow(x.Value - avg, 2)).Average())
                    });
                }
                else
                {
                    weeklyTrends.Add(new DTOs.GlycemicTrendDto
                    {
                        Period = periodKey,
                        Average = 0,
                        Min = 0,
                        Max = 0,
                        StdDev = 0
                    });
                }
            }
            dto.GlycemicTrends = weeklyTrends;

            // Calcolo aggregato min, max, stddev sulle ultime 4 settimane (tutti i valori)
            var glycemicLast4Weeks = glycemic.Where(g => g.MeasurementDateTime.Date >= last4WeeksMonday).ToList();
            double? min4w = glycemicLast4Weeks.Any() ? glycemicLast4Weeks.Min(g => (double?)g.Value) : null;
            double? max4w = glycemicLast4Weeks.Any() ? glycemicLast4Weeks.Max(g => (double?)g.Value) : null;
            double? avg4w = glycemicLast4Weeks.Any() ? glycemicLast4Weeks.Average(g => (double)g.Value) : null;
            double? stddev4w = null;
            if (glycemicLast4Weeks.Any() && avg4w.HasValue)
            {
                stddev4w = Math.Sqrt(glycemicLast4Weeks.Select(x => Math.Pow(x.Value - avg4w.Value, 2)).Average());
            }
            dto.GlycemicLast4WeeksStats = new GlycemicStatsDto
            {
                Min = min4w,
                Max = max4w,
                StdDev = stddev4w
            };

            // Distribuzione valori glicemici (istogramma, boxplot)
            if (glycemic.Any())
            {
                var values = glycemic.Select(g => g.Value).OrderBy(x => x).ToList();
                int n = values.Count;
                double median = n % 2 == 1 ? values[n / 2] : (values[n / 2 - 1] + values[n / 2]) / 2.0;
                double q1 = values[(int)(n * 0.25)];
                double q3 = values[(int)(n * 0.75)];
                double min = values.First();
                double max = values.Last();
                var iqr = q3 - q1;
                var outliers = values.Where(v => v < q1 - 1.5 * iqr || v > q3 + 1.5 * iqr).ToList();
                dto.GlycemicDistribution = new DTOs.GlycemicDistributionDto
                {
                    Values = values.Select(v => (int)v).ToList(),
                    Q1 = q1,
                    Median = median,
                    Q3 = q3,
                    Min = min,
                    Max = max,
                    Outliers = outliers.Select(v => (int)v).ToList(),
                    TargetMin = 70,
                    TargetMax = 180
                };
            }

            // Adesione terapia: % assunzioni programmate vs effettive (ultimi 30 giorni)
            var lastMonth = now.AddDays(-30);
            // Conta solo le MedicationSchedules di terapie attive nel periodo (StartDate <= giorno <= EndDate/null)
            var therapies = await _context.Therapies
                .Where(t => t.UserId == userId && t.StartDate <= DateOnly.FromDateTime(now) && (t.EndDate == null || t.EndDate >= DateOnly.FromDateTime(lastMonth)))
                .ToListAsync();

            int scheduled = 0;
            foreach (var therapy in therapies)
            {
                // Per ogni giorno in cui la terapia è attiva nell'ultimo mese
                var therapyStart = therapy.StartDate ?? DateOnly.FromDateTime(now);
                var therapyEnd = therapy.EndDate ?? DateOnly.FromDateTime(now);
                var from = therapyStart > DateOnly.FromDateTime(lastMonth) ? therapyStart : DateOnly.FromDateTime(lastMonth);
                var to = therapy.EndDate == null || therapyEnd > DateOnly.FromDateTime(now) ? DateOnly.FromDateTime(now) : therapyEnd;
                for (var day = from; day <= to; day = day.AddDays(1))
                {
                    // Ogni giorno, conta tutte le MedicationSchedules della terapia
                    var ms = await _context.MedicationSchedules.Where(msc => msc.TherapyId == therapy.TherapyId).ToListAsync();
                    scheduled += ms.Count;
                }
            }
            var performed = await _context.MedicationIntakes
                .Where(mi => mi.UserId == userId && mi.IntakeDateTime >= lastMonth && mi.MedicationScheduleId != null)
                .CountAsync();
            dto.TherapyAdherence = new DTOs.TherapyAdherenceDto
            {
                ScheduledIntakes = scheduled,
                PerformedIntakes = performed,
                AdherencePercent = scheduled > 0 ? Math.Round(100.0 * performed / scheduled, 1) : 0
            };

            // Sintomi recenti (ultimi 30 giorni)
            dto.RecentSymptoms = await _context.Symptoms
                .Where(s => s.UserId == userId && s.OccurredAt >= lastMonth)
                .OrderByDescending(s => s.OccurredAt)
                .Select(s => new DTOs.SymptomDto
                {
                    Description = s.Description ?? string.Empty,
                    OccurredAt = s.OccurredAt
                })
                .ToListAsync();

            // Alert clinici recenti (ultimi 30 giorni)
            dto.RecentAlerts = await _context.AlertRecipients
                .Where(ar => ar.RecipientUserId == userId && ar.Alert.CreatedAt >= lastMonth)
                .OrderByDescending(ar => ar.Alert.CreatedAt)
                .Select(ar => new DTOs.AlertDto
                {
                    Type = ar.Alert.AlertType.Label,
                    Message = ar.Alert.Message,
                    CreatedAt = ar.Alert.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            // Comorbidità
            dto.Comorbidities = await _context.ClinicalComorbidities
                .Where(c => c.UserId == userId)
                .Select(c => new DTOs.ComorbidityDto
                {
                    Id = c.ClinicalComorbidityId,
                    Comorbidity = c.Comorbidity ?? string.Empty,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate
                })
                .ToListAsync();

            // Fattori di rischio
            dto.RiskFactors = await _context.PatientRiskFactors
                .Where(prf => prf.UserId == userId)
                .Select(prf => prf.RiskFactor)
                .ToListAsync();

            // Assunzioni farmaci extra-terapia recenti (ultimi 30 giorni)
            dto.RecentExtraMedicationIntakes = await _context.MedicationIntakes
                .Where(mi => mi.UserId == userId && mi.IntakeDateTime >= lastMonth && mi.MedicationScheduleId == null)
                .OrderByDescending(mi => mi.IntakeDateTime)
                .Select(mi => new DTOs.ExtraMedicationIntakeDto
                {
                    MedicationName = mi.MedicationTakenName ?? string.Empty,
                    Quantity = (double)mi.ExpectedQuantityValue,
                    Unit = mi.Unit ?? string.Empty,
                    IntakeDateTime = mi.IntakeDateTime,
                    Note = mi.Note
                })
                .ToListAsync();

            return Ok(dto);
        }

        [HttpPost("update-comorbidities-riskfactors")]
        public async Task<IActionResult> UpdateComorbiditiesAndRiskFactors([FromBody] UpdateComorbiditiesAndRiskFactorsDto dto)
        {
            if (dto == null || dto.UserId <= 0)
                return BadRequest("Invalid data.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Recupera le comorbidità esistenti per l'utente
                var existingComorbidities = await _context.ClinicalComorbidities
                    .Where(c => c.UserId == dto.UserId)
                    .ToListAsync();

                // Aggiungi solo le nuove comorbidità (stessa comorbidity e start date considerate "uguali")
                foreach (var c in dto.Comorbidities)
                {
                    bool alreadyExists = existingComorbidities.Any(ec =>
                        ec.ClinicalComorbidityId == c.Id
                    );
                    if (!alreadyExists)
                    {
                        var newComorbidity = new ClinicalComorbidities
                        {
                            UserId = dto.UserId,
                            Comorbidity = c.Comorbidity,
                            StartDate = c.StartDate,
                            EndDate = c.EndDate
                        };
                        _context.ClinicalComorbidities.Add(newComorbidity);
                    }
                    else
                    {
                        // Aggiorna la comorbidità esistente se necessario
                        var existingComorbidity = existingComorbidities.First(ec => ec.ClinicalComorbidityId == c.Id);
                        existingComorbidity.Comorbidity = c.Comorbidity;
                        existingComorbidity.StartDate = c.StartDate;
                        existingComorbidity.EndDate = c.EndDate;
                        _context.ClinicalComorbidities.Update(existingComorbidity);
                    }
                }
                await _context.SaveChangesAsync();

                // Recupera i risk factors esistenti per l'utente
                var existingRiskFactors = await _context.PatientRiskFactors
                    .Where(rf => rf.UserId == dto.UserId)
                    .ToListAsync();

                // Aggiungi solo i nuovi risk factors
                foreach (var riskFactorId in dto.RiskFactorIds)
                {
                    bool alreadyExists = existingRiskFactors.Any(rf => rf.RiskFactorId == riskFactorId);
                    if (!alreadyExists)
                    {
                        var newRisk = new PatientRiskFactors
                        {
                            UserId = dto.UserId,
                            RiskFactorId = riskFactorId
                        };
                        _context.PatientRiskFactors.Add(newRisk);
                    }
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok("Comorbidities and risk factors added successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un fattore di rischio specifico per un utente.
        /// </summary>
        [HttpDelete("delete-user-riskfactor")]
        public async Task<IActionResult> DeleteUserRiskFactor([FromQuery] int userId, [FromQuery] int riskFactorId)
        {
            if (userId <= 0 || riskFactorId <= 0)
                return BadRequest("Invalid userId or riskFactorId.");

            var risk = await _context.PatientRiskFactors.FirstOrDefaultAsync(rf => rf.UserId == userId && rf.RiskFactorId == riskFactorId);
            if (risk == null)
                return NotFound("Risk factor not found for this user.");

            _context.PatientRiskFactors.Remove(risk);
            await _context.SaveChangesAsync();
            return Ok("Risk factor removed for user.");
        }

        /// <summary>
        /// Elimina una comorbidità specifica per un utente.
        /// </summary>
        [HttpDelete("delete-user-comorbidity")]
        public async Task<IActionResult> DeleteUserComorbidity([FromQuery] int userId, [FromQuery] string comorbidity, [FromQuery] string? startDate)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(comorbidity) || string.IsNullOrWhiteSpace(startDate))
                return BadRequest("Invalid parameters.");

            if (!DateOnly.TryParse(startDate, out var parsedStartDate))
                return BadRequest("Invalid startDate format. Use yyyy-MM-dd.");

            var com = await _context.ClinicalComorbidities.FirstOrDefaultAsync(c => c.UserId == userId && c.Comorbidity == comorbidity && c.StartDate == parsedStartDate);
            if (com == null)
                return NotFound("Comorbidity not found for this user.");

            _context.ClinicalComorbidities.Remove(com);
            await _context.SaveChangesAsync();
            return Ok("Comorbidity removed for user.");
        }
    }
}
