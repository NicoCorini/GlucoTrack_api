using GlucoTrack_api.Models;
using GlucoTrack_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlucoTrack_api.DTOs;
using GlucoTrack_api.Utils;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DoctorController : Controller
    {
        private readonly GlucoTrackDBContext _context;
        private readonly ChangeLogService _changeLogService;

        public DoctorController(GlucoTrackDBContext context, ChangeLogService changeLogService)
        {
            _context = context;
            _changeLogService = changeLogService;
        }

        /// <summary>
        /// Returns a dashboard summary for the doctor, including:
        /// - Patients who are currently "in line" (within glycemic target and no open glycemic alerts)
        /// - Patients with high weekly average glycemia
        /// - All open glycemic alerts where the doctor is a recipient
        ///
        /// The summary includes patient trends, alert counts by severity, and details for each open alert.
        /// </summary>
        [HttpGet("dashboard-summary")]
        public async Task<ActionResult<DoctorDashboardSummaryDto>> GetDoctorDashboardSummary([FromQuery] int doctorId)
        {
            if (doctorId <= 0)
                return BadRequest("Invalid doctorId.");

            // 1. Find all patients associated with the doctor (PatientDoctors relationship, active)
            var patientIds = await _context.PatientDoctors
                .Where(pd => pd.DoctorId == doctorId && (pd.EndDate == null || pd.EndDate > DateOnly.FromDateTime(DateTime.UtcNow)))
                .Select(pd => pd.PatientId)
                .Distinct()
                .ToListAsync();

            // 2. Retrieve basic patient info
            var patients = await _context.Users
                .Where(u => patientIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.FirstName, u.LastName })
                .ToListAsync();

            // 3. Calculate weekly glycemic average and trend for each patient
            var now = DateTime.UtcNow;
            var lastWeekMonday = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday); // Monday of this week
            var prevWeekMonday = lastWeekMonday.AddDays(-7);

            // Retrieve all glycemic measurements from the last 2 weeks for these patients
            var glycemic = await _context.GlycemicMeasurements
                .Where(g => patientIds.Contains(g.UserId) && g.MeasurementDateTime >= prevWeekMonday)
                .ToListAsync();

            var inLine = new List<DashboardPatientSummaryDto>();
            var highAvg = new List<DashboardPatientSummaryDto>();

            foreach (var p in patients)
            {
                var weekData = glycemic.Where(g => g.UserId == p.UserId && g.MeasurementDateTime.Date >= lastWeekMonday).ToList();
                var prevWeekData = glycemic.Where(g => g.UserId == p.UserId && g.MeasurementDateTime.Date >= prevWeekMonday && g.MeasurementDateTime.Date < lastWeekMonday).ToList();
                double avg = weekData.Any() ? weekData.Average(x => x.Value) : 0;
                double prevAvg = prevWeekData.Any() ? prevWeekData.Average(x => x.Value) : 0;
                string trend = "stable";
                if (weekData.Any() && prevWeekData.Any())
                {
                    if (avg > prevAvg + 5) trend = "up";
                    else if (avg < prevAvg - 5) trend = "down";
                }

                // Check if the patient has open glycemic alerts
                bool hasOpenGlyAlert = await _context.AlertRecipients
                    .AnyAsync(ar => ar.RecipientUserId == p.UserId && ar.Alert.Status != "resolved" &&
                        (ar.Alert.AlertType.Label == "CRITICAL_GLUCOSE" || ar.Alert.AlertType.Label == "VERY_HIGH_GLUCOSE" || ar.Alert.AlertType.Label == "HIGH_GLUCOSE"));

                var dto = new DashboardPatientSummaryDto
                {
                    UserId = p.UserId,
                    FirstName = p.FirstName ?? string.Empty,
                    LastName = p.LastName ?? string.Empty,
                    WeeklyAvgGlycemia = Math.Round(avg, 1),
                    Trend = trend
                };

                if (!hasOpenGlyAlert && avg <= 180)
                    inLine.Add(dto);
                else if (avg > 180)
                    highAvg.Add(dto);
            }

            // 4. Retrieve all open glycemic alerts where the doctor is the recipient
            var openAlerts = await _context.AlertRecipients
                .Where(ar => ar.RecipientUserId == doctorId && ar.Alert.Status != "resolved" &&
                    (ar.Alert.AlertType.Label == "CRITICAL_GLUCOSE" || ar.Alert.AlertType.Label == "VERY_HIGH_GLUCOSE" || ar.Alert.AlertType.Label == "HIGH_GLUCOSE"))
                .Include(ar => ar.Alert)
                    .ThenInclude(a => a.AlertType)
                .Include(ar => ar.Alert.User)
                .OrderByDescending(ar => ar.Alert.CreatedAt)
                .ToListAsync();

            int critical = 0, severe = 0, mild = 0;
            var alertDetails = new List<DashboardAlertDetailDto>();
            foreach (var ar in openAlerts)
            {
                string level = ar.Alert.AlertType.Label switch
                {
                    "CRITICAL_GLUCOSE" => "CRITICAL",
                    "VERY_HIGH_GLUCOSE" => "SEVERE",
                    "HIGH_GLUCOSE" => "MILD",
                    _ => ""
                };
                if (level == "CRITICAL") critical++;
                else if (level == "SEVERE") severe++;
                else if (level == "MILD") mild++;

                alertDetails.Add(new DashboardAlertDetailDto
                {
                    AlertRecipientId = ar.AlertRecipientId,
                    AlertId = ar.AlertId,
                    PatientId = ar.Alert.UserId,
                    PatientFirstName = ar.Alert.User?.FirstName ?? string.Empty,
                    PatientLastName = ar.Alert.User?.LastName ?? string.Empty,
                    Level = level,
                    Message = ar.Alert.Message ?? string.Empty,
                    CreatedAt = ar.Alert.CreatedAt,
                    Status = ar.Alert.Status ?? string.Empty
                });
            }

            var alertSummary = new DashboardAlertSummaryDto
            {
                TotalOpen = openAlerts.Count,
                CriticalCount = critical,
                SevereCount = severe,
                MildCount = mild,
                Alerts = alertDetails
            };

            var result = new DoctorDashboardSummaryDto
            {
                InLinePatients = inLine,
                HighAvgGlucosePatients = highAvg,
                GlycemiaAlerts = alertSummary
            };

            return Ok(result);
        }

        /// <summary>
        /// Returns the 10 most recent therapies created by the specified doctor, including their medication schedules.
        ///
        /// Each therapy includes its details and a list of associated medication schedules. If no therapies are found, returns 404.
        /// </summary>
        [HttpGet("recent-therapies")]
        public async Task<IActionResult> GetDoctorRecentTherapies([FromQuery] int doctorId)
        {
            // Only active therapies (EndDate == null)
            var recentTherapiesRaw = await _context.Therapies
                .Where(t => t.DoctorId == doctorId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentTherapies = recentTherapiesRaw.Select(t => new RecentTherapyDto
            {
                TherapyId = t.TherapyId,
                Title = t.Title ?? string.Empty,
                Instructions = t.Instructions ?? string.Empty,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                DoctorId = t.DoctorId,
                UserId = t.UserId,
                CreatedAt = t.CreatedAt,
                MedicationSchedules = _context.MedicationSchedules
                    .Where(ms => ms.TherapyId == t.TherapyId)
                    .Select(ms => new RecentMedicationScheduleDto
                    {
                        MedicationScheduleId = ms.MedicationScheduleId,
                        MedicationName = ms.MedicationName,
                        Quantity = ms.Quantity,
                        Unit = ms.Unit,
                        DailyIntakes = ms.DailyIntakes
                    })
                    .ToList()
            }).ToList();

            if (recentTherapies == null || !recentTherapies.Any())
                return NotFound("No recent therapies found for this doctor.");

            return Ok(recentTherapies);
        }

        /// <summary>
        /// Returns a paginated list of patients, with optional filters for search, age, gender, and doctor-patient relationship.
        ///
        /// - If onlyDoctorPatients is true, only patients currently associated with the specified doctor are returned.
        /// - Supports text search on first name, last name, or email.
        /// - Filters by gender and age range (minAge, maxAge).
        /// - Returns 10 patients per page. If no patients match, returns 404.
        /// </summary>
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
                .Skip(page * 10)
                .Take(10)
                .ToListAsync();

            if (!result.Any())
                return NotFound("No patients found matching the criteria.");

            return Ok(result);
        }

        /// <summary>
        /// Creates a new therapy or updates an existing one for a patient, including medication schedules.
        ///
        /// - If TherapyId is provided and valid, updates the existing therapy:
        ///   - If the therapy has not started yet, performs a hard delete and creates a new therapy starting tomorrow.
        ///   - If the therapy has started, performs a soft close (sets EndDate to today) and creates a new therapy starting tomorrow, linked to the previous one.
        /// - If TherapyId is not provided, creates a new therapy starting tomorrow with the provided details and medication schedules.
        /// - All insert, update, and delete operations are logged in the ChangeLogs table.
        /// - Returns 200 OK on success, or an error message on failure.
        /// </summary>
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

                    var today = DateOnly.FromDateTime(DateTime.UtcNow);

                    // Serializza lo stato prima della modifica/cancellazione
                    var before = new
                    {
                        oldTherapy.TherapyId,
                        oldTherapy.DoctorId,
                        oldTherapy.UserId,
                        oldTherapy.Title,
                        oldTherapy.Instructions,
                        oldTherapy.StartDate,
                        oldTherapy.EndDate,
                        oldTherapy.PreviousTherapyId,
                        oldTherapy.CreatedAt
                    };

                    if (oldTherapy.StartDate > today)
                    {
                        // Hard delete della terapia vuota
                        var medicationSchedules = await _context.MedicationSchedules
                            .Where(ms => ms.TherapyId == oldTherapy.TherapyId)
                            .ToListAsync();

                        // Logga la cancellazione dei medication schedules
                        foreach (var ms in medicationSchedules)
                        {
                            var msBefore = new
                            {
                                ms.MedicationScheduleId,
                                ms.TherapyId,
                                ms.MedicationName,
                                ms.Quantity,
                                ms.Unit,
                                ms.DailyIntakes
                            };
                            await _changeLogService.LogChangeAsync(
                                doctorId: oldTherapy.DoctorId,
                                tableName: "MedicationSchedules",
                                recordId: ms.MedicationScheduleId,
                                action: "Delete",
                                before: msBefore,
                                after: null
                            );
                        }

                        _context.MedicationSchedules.RemoveRange(medicationSchedules);
                        await _context.SaveChangesAsync();

                        _context.Therapies.Remove(oldTherapy);
                        await _context.SaveChangesAsync();

                        // Logga la cancellazione
                        await _changeLogService.LogChangeAsync(
                            doctorId: oldTherapy.DoctorId,
                            tableName: "Therapies",
                            recordId: oldTherapy.TherapyId,
                            action: "Delete",
                            before: before,
                            after: null
                        );
                    }
                    else
                    {
                        // Soft close della terapia
                        oldTherapy.EndDate = today;
                        _context.Therapies.Update(oldTherapy);
                        await _context.SaveChangesAsync();

                        // Serializza lo stato dopo la modifica
                        var after = new
                        {
                            oldTherapy.TherapyId,
                            oldTherapy.DoctorId,
                            oldTherapy.UserId,
                            oldTherapy.Title,
                            oldTherapy.Instructions,
                            oldTherapy.StartDate,
                            oldTherapy.EndDate,
                            oldTherapy.PreviousTherapyId,
                            oldTherapy.CreatedAt
                        };

                        // Logga la modifica
                        await _changeLogService.LogChangeAsync(
                            doctorId: oldTherapy.DoctorId,
                            tableName: "Therapies",
                            recordId: oldTherapy.TherapyId,
                            action: "SoftDelete",
                            before: before,
                            after: after
                        );
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

                    // Logga l'inserimento della nuova terapia
                    var afterInsert = new
                    {
                        newTherapy.TherapyId,
                        newTherapy.DoctorId,
                        newTherapy.UserId,
                        newTherapy.Title,
                        newTherapy.Instructions,
                        newTherapy.StartDate,
                        newTherapy.EndDate,
                        newTherapy.PreviousTherapyId,
                        newTherapy.CreatedAt
                    };
                    await _changeLogService.LogChangeAsync(
                        doctorId: newTherapy.DoctorId,
                        tableName: "Therapies",
                        recordId: newTherapy.TherapyId,
                        action: "Insert",
                        before: null,
                        after: afterInsert
                    );

                    // Inserisci le medication schedules per la nuova terapia
                    foreach (var msDto in dto.MedicationSchedules)
                    {
                        var ms = new MedicationSchedules
                        {
                            TherapyId = newTherapy.TherapyId,
                            MedicationName = msDto.MedicationName,
                            Quantity = msDto.Quantity,
                            Unit = msDto.Unit,
                            DailyIntakes = msDto.DailyIntakes
                        };
                        _context.MedicationSchedules.Add(ms);
                        await _context.SaveChangesAsync();
                        // Logga l'inserimento del medication schedule
                        var msAfter = new
                        {
                            ms.MedicationScheduleId,
                            ms.TherapyId,
                            ms.MedicationName,
                            ms.Quantity,
                            ms.Unit,
                            ms.DailyIntakes
                        };
                        await _changeLogService.LogChangeAsync(
                            doctorId: newTherapy.DoctorId,
                            tableName: "MedicationSchedules",
                            recordId: ms.MedicationScheduleId,
                            action: "Insert",
                            before: null,
                            after: msAfter
                        );
                    }
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

                    // Logga l'inserimento della nuova terapia
                    var afterInsert = new
                    {
                        therapyToAdd.TherapyId,
                        therapyToAdd.DoctorId,
                        therapyToAdd.UserId,
                        therapyToAdd.Title,
                        therapyToAdd.Instructions,
                        therapyToAdd.StartDate,
                        therapyToAdd.EndDate,
                        therapyToAdd.PreviousTherapyId,
                        therapyToAdd.CreatedAt
                    };
                    await _changeLogService.LogChangeAsync(
                        doctorId: therapyToAdd.DoctorId,
                        tableName: "Therapies",
                        recordId: therapyToAdd.TherapyId,
                        action: "Insert",
                        before: null,
                        after: afterInsert
                    );

                    foreach (var msDto in dto.MedicationSchedules)
                    {
                        var ms = new MedicationSchedules
                        {
                            TherapyId = therapyToAdd.TherapyId,
                            MedicationName = msDto.MedicationName,
                            Quantity = msDto.Quantity,
                            Unit = msDto.Unit,
                            DailyIntakes = msDto.DailyIntakes
                        };
                        _context.MedicationSchedules.Add(ms);
                        await _context.SaveChangesAsync();
                        // Logga l'inserimento del medication schedule
                        var msAfter = new
                        {
                            ms.MedicationScheduleId,
                            ms.TherapyId,
                            ms.MedicationName,
                            ms.Quantity,
                            ms.Unit,
                            ms.DailyIntakes
                        };
                        await _changeLogService.LogChangeAsync(
                            doctorId: therapyToAdd.DoctorId,
                            tableName: "MedicationSchedules",
                            recordId: ms.MedicationScheduleId,
                            action: "Insert",
                            before: null,
                            after: msAfter
                        );
                    }
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

        /// <summary>
        /// Returns the details of a specific therapy, including its medication schedules and patient information.
        ///
        /// - Requires a valid therapyId as a query parameter.
        /// - Returns therapy details, a list of associated medication schedules, and basic patient info.
        /// - If the therapy is not found, returns 404.
        /// </summary>
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
                            ms.Quantity,
                            ms.Unit,
                            ms.DailyIntakes
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

        /// <summary>
        /// Performs a soft delete of a therapy by setting its EndDate to today, without removing it from the database.
        ///
        /// - Requires a valid therapyId as a query parameter.
        /// - Updates the EndDate of the therapy to today, keeping the record for historical purposes.
        /// - Logs the change in the ChangeLogs table with before/after states and action "SoftDelete".
        /// - Returns 200 OK on success, or an error message on failure.
        /// </summary>
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
                // 1. Serializza lo stato prima della modifica
                var before = new
                {
                    therapy.TherapyId,
                    therapy.DoctorId,
                    therapy.UserId,
                    therapy.Title,
                    therapy.Instructions,
                    therapy.StartDate,
                    therapy.EndDate,
                    therapy.PreviousTherapyId,
                    therapy.CreatedAt
                };

                // Imposta la data di fine a oggi (soft delete, non elimina dal DB)
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                therapy.EndDate = today;
                _context.Therapies.Update(therapy);
                await _context.SaveChangesAsync();

                // 2. Serializza lo stato dopo la modifica
                var after = new
                {
                    therapy.TherapyId,
                    therapy.DoctorId,
                    therapy.UserId,
                    therapy.Title,
                    therapy.Instructions,
                    therapy.StartDate,
                    therapy.EndDate,
                    therapy.PreviousTherapyId,
                    therapy.CreatedAt
                };

                // 3. Logga la modifica tramite ChangeLogService
                await _changeLogService.LogChangeAsync(
                    doctorId: therapy.DoctorId,
                    tableName: "Therapies",
                    recordId: therapy.TherapyId,
                    action: "SoftDelete",
                    before: before,
                    after: after
                );

                return Ok("Therapy end date set to today (soft delete). Therapy remains in history.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Permanently deletes a therapy and all its related medication schedules from the database (hard delete).
        ///
        /// - Requires a valid <paramref name="therapyId"/> as a query parameter.
        /// - Removes the therapy and all associated medication schedules from the database.
        /// - Logs the deletion in the ChangeLogs table with the state before deletion (after = null).
        /// - This operation is irreversible; the records are physically removed.
        /// - Returns 200 OK on success, 404 if the therapy is not found, or 400 for invalid parameters.
        /// </summary>
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
                // 1. Serialize the state before deletion for audit logging
                var before = new
                {
                    therapy.TherapyId,
                    therapy.DoctorId,
                    therapy.UserId,
                    therapy.Title,
                    therapy.Instructions,
                    therapy.StartDate,
                    therapy.EndDate,
                    therapy.PreviousTherapyId,
                    therapy.CreatedAt
                };

                // Remove the therapy and all related medication schedules from the database
                _context.Therapies.Remove(therapy);
                var medicationSchedules = await _context.MedicationSchedules
                    .Where(ms => ms.TherapyId == therapyId)
                    .ToListAsync();
                _context.MedicationSchedules.RemoveRange(medicationSchedules);
                await _context.SaveChangesAsync();

                // 2. Log the deletion using ChangeLogService (after = null)
                await _changeLogService.LogChangeAsync(
                    doctorId: therapy.DoctorId,
                    tableName: "Therapies",
                    recordId: therapy.TherapyId,
                    action: "Delete",
                    before: before,
                    after: null
                );

                return Ok("Therapy and related medication schedules deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns a comprehensive analytics summary for a specific patient, including glycemic trends, therapy adherence, recent symptoms, alerts, comorbidities, risk factors, and extra-medication intakes.
        ///
        /// - Retrieves patient profile and demographics.
        /// - Computes glycemic trends (weekly, monthly), statistics, and distribution for the last 6 months.
        /// - Calculates therapy adherence percentage based on scheduled vs. performed intakes.
        /// - Returns the 10 most recent symptoms and clinical alerts.
        /// - Lists all comorbidities and risk factors for the patient.
        /// - Includes recent extra-medication intakes (last 30 days).
        /// </summary>
        /// <param name="userId">The ID of the patient whose analytics are requested.</param>
        /// <returns>200 OK with a <see cref="PatientAnalyticsDto"/> containing analytics data; 404 if the patient is not found.</returns>
        [HttpGet("patient-analytics")]
        public async Task<IActionResult> GetPatientAnalytics([FromQuery] int userId)
        {
            // Retrieve basic patient information
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
            var fromDate = now.AddMonths(-6); // last 6 months
            var glycemic = await _context.GlycemicMeasurements
                .Where(g => g.UserId == userId && g.MeasurementDateTime >= fromDate)
                .ToListAsync();

            // Group by week (last 4 weeks)
            var last4WeeksMonday = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday).AddDays(-21); // Monday 4 weeks ago
            // Build the last 4 weeks (even if empty)
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

            // Aggregate calculation: min, max, stddev over the last 4 weeks (all values)
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

            // Glycemic value distribution (histogram, boxplot)
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

            // Therapy adherence: % of scheduled vs. performed intakes (from the beginning)
            // Get all therapies for the patient (no time filter)
            var therapies = await _context.Therapies
                .Where(t => t.UserId == userId)
                .ToListAsync();

            int scheduled = 0;
            int performed = 0;
            foreach (var therapy in therapies)
            {
                var therapyStart = therapy.StartDate;
                var therapyEnd = therapy.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var to = therapy.EndDate == null || therapyEnd > DateOnly.FromDateTime(DateTime.UtcNow)
                    ? DateOnly.FromDateTime(DateTime.UtcNow)
                    : therapyEnd;
                int daysCount = (to.DayNumber - therapyStart.DayNumber) + 1;
                if (daysCount < 1) continue;

                var msList = await _context.MedicationSchedules.Where(msc => msc.TherapyId == therapy.TherapyId).ToListAsync();
                foreach (var ms in msList)
                {
                    int daily = ms.DailyIntakes > 0 ? ms.DailyIntakes : 1;
                    scheduled += daily * daysCount;

                    // Count only the intakes for this schedule, within the therapy period
                    performed += await _context.MedicationIntakes
                        .Where(mi =>
                            mi.UserId == userId &&
                            mi.MedicationScheduleId == ms.MedicationScheduleId &&
                            mi.IntakeDateTime.Date >= therapyStart.ToDateTime(TimeOnly.MinValue).Date &&
                            mi.IntakeDateTime.Date <= to.ToDateTime(TimeOnly.MaxValue).Date
                        )
                        .CountAsync();
                }
            }
            dto.TherapyAdherence = new TherapyAdherenceDto
            {
                ScheduledIntakes = scheduled,
                PerformedIntakes = performed,
                AdherencePercent = scheduled > 0 ? Math.Round(100.0 * performed / scheduled, 1) : 0
            };

            // Last 10 symptoms
            dto.RecentSymptoms = await _context.Symptoms
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.OccurredAt)
                .Take(10)
                .Select(s => new DTOs.SymptomDto
                {
                    Description = s.Description ?? string.Empty,
                    OccurredAt = s.OccurredAt
                })
                .ToListAsync();

            // Last 10 clinical alerts
            dto.RecentAlerts = await _context.AlertRecipients
                .Where(ar => ar.RecipientUserId == userId)
                .Include(ar => ar.Alert)
                    .ThenInclude(a => a.AlertType)
                .OrderByDescending(ar => ar.Alert.CreatedAt)
                .Take(10)
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

            // Comorbidities
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

            // Risk factors
            dto.RiskFactors = await _context.PatientRiskFactors
                .Where(prf => prf.UserId == userId)
                .Select(prf => prf.RiskFactor)
                .ToListAsync();

            // Recent extra-therapy medication intakes (last 30 days)
            dto.RecentExtraMedicationIntakes = await _context.MedicationIntakes
                .Where(mi => mi.UserId == userId && mi.MedicationScheduleId == null)
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

                // Log delle comorbidità già esistenti per update
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
                        await _context.SaveChangesAsync();
                        // Log insert
                        var after = new
                        {
                            newComorbidity.ClinicalComorbidityId,
                            newComorbidity.UserId,
                            newComorbidity.Comorbidity,
                            newComorbidity.StartDate,
                            newComorbidity.EndDate
                        };
                        await _changeLogService.LogChangeAsync(
                            doctorId: dto.UserId,
                            tableName: "ClinicalComorbidities",
                            recordId: newComorbidity.ClinicalComorbidityId,
                            action: "Insert",
                            before: null,
                            after: after
                        );
                    }
                    else
                    {
                        // Update the existing comorbidity if needed
                        var existingComorbidity = existingComorbidities.First(ec => ec.ClinicalComorbidityId == c.Id);
                        // Log before update
                        var before = new
                        {
                            existingComorbidity.ClinicalComorbidityId,
                            existingComorbidity.UserId,
                            existingComorbidity.Comorbidity,
                            existingComorbidity.StartDate,
                            existingComorbidity.EndDate
                        };
                        existingComorbidity.Comorbidity = c.Comorbidity;
                        existingComorbidity.StartDate = c.StartDate;
                        existingComorbidity.EndDate = c.EndDate;
                        _context.ClinicalComorbidities.Update(existingComorbidity);
                        await _context.SaveChangesAsync();
                        // Log after update
                        var after = new
                        {
                            existingComorbidity.ClinicalComorbidityId,
                            existingComorbidity.UserId,
                            existingComorbidity.Comorbidity,
                            existingComorbidity.StartDate,
                            existingComorbidity.EndDate
                        };
                        await _changeLogService.LogChangeAsync(
                            doctorId: dto.UserId,
                            tableName: "ClinicalComorbidities",
                            recordId: existingComorbidity.ClinicalComorbidityId,
                            action: "Update",
                            before: before,
                            after: after
                        );
                    }
                }

                // Retrieve existing risk factors for the user
                var existingRiskFactors = await _context.PatientRiskFactors
                    .Where(rf => rf.UserId == dto.UserId)
                    .ToListAsync();

                // Add only new risk factors
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
                        await _context.SaveChangesAsync();
                        // Log insert
                        var after = new
                        {
                            newRisk.UserId,
                            newRisk.RiskFactorId
                        };
                        await _changeLogService.LogChangeAsync(
                            doctorId: dto.UserId,
                            tableName: "PatientRiskFactors",
                            recordId: newRisk.RiskFactorId,
                            action: "Insert",
                            before: null,
                            after: after
                        );
                    }
                }

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
        /// Deletes a specific risk factor for a user.
        /// </summary>
        [HttpDelete("delete-user-riskfactor")]
        public async Task<IActionResult> DeleteUserRiskFactor([FromQuery] int userId, [FromQuery] int riskFactorId)
        {
            if (userId <= 0 || riskFactorId <= 0)
                return BadRequest("Invalid userId or riskFactorId.");

            var risk = await _context.PatientRiskFactors.FirstOrDefaultAsync(rf => rf.UserId == userId && rf.RiskFactorId == riskFactorId);
            if (risk == null)
                return NotFound("Risk factor not found for this user.");

            // 1. Serialize the state before deletion
            var before = new
            {
                risk.UserId,
                risk.RiskFactorId
            };

            _context.PatientRiskFactors.Remove(risk);
            await _context.SaveChangesAsync();

            // 2. Log the deletion using ChangeLogService (DetailsAfter = null)
            await _changeLogService.LogChangeAsync(
                doctorId: userId, // oppure recupera il doctorId se disponibile
                tableName: "PatientRiskFactors",
                recordId: risk.RiskFactorId,
                action: "Delete",
                before: before,
                after: null
            );

            return Ok("Risk factor removed for user.");
        }

        /// <summary>
        /// Deletes a specific comorbidity for a user.
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

            // 1. Serialize the state before deletion
            var before = new
            {
                com.ClinicalComorbidityId,
                com.UserId,
                com.Comorbidity,
                com.StartDate,
                com.EndDate
            };

            _context.ClinicalComorbidities.Remove(com);
            await _context.SaveChangesAsync();

            // 2. Log the deletion using ChangeLogService (DetailsAfter = null)
            await _changeLogService.LogChangeAsync(
                doctorId: userId, // oppure recupera il doctorId se disponibile
                tableName: "ClinicalComorbidities",
                recordId: com.ClinicalComorbidityId,
                action: "Delete",
                before: before,
                after: null
            );

            return Ok("Comorbidity removed for user.");
        }
    }
}
