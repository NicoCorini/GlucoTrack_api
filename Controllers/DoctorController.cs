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
    }
}
