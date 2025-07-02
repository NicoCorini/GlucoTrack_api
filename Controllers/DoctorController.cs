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


            var recentTherapiesRaw = await _context.Therapies
                .Where(t => t.DoctorId == doctorId)
                .OrderByDescending(t => t.UpdatedAt != null ? t.UpdatedAt : t.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentTherapies = recentTherapiesRaw.Select(t => new RecentTherapyDto
            {
                TherapyId = t.TherapyId,
                Instructions = t.Instructions ?? string.Empty,
                StartDate = t.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = t.EndDate,
                DoctorId = t.DoctorId,
                UserId = t.UserId,
                CreatedAt = t.CreatedAt ?? DateTime.MinValue,
                UpdatedAt = t.UpdatedAt,
                MedicationSchedules = _context.MedicationSchedules
                    .Where(ms => ms.TherapyId == t.TherapyId)
                    .Select(ms => new RecentMedicationScheduleDto
                    {
                        MedicationScheduleId = ms.MedicationScheduleId,
                        MedicationName = ms.MedicationName ?? string.Empty,
                        ExpectedQuantity = (double)ms.ExpectedQuantity,
                        ExpectedUnit = ms.ExpectedUnit ?? string.Empty,
                        ScheduledDateTime = ms.ScheduledDateTime.ToString("yyyy-MM-ddTHH:mm:ss")
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
                Therapies? therapy = null;
                bool isUpdate = dto.TherapyId.HasValue && dto.TherapyId.Value > 0;
                if (isUpdate)
                {
                    int therapyId = dto.TherapyId ?? 0;
                    therapy = await _context.Therapies.FirstOrDefaultAsync(t => t.TherapyId == therapyId);
                    if (therapy == null)
                        return NotFound("Therapy not found.");

                    // Aggiorna i campi base
                    therapy.Instructions = dto.Instructions;
                    therapy.StartDate = DateOnly.FromDateTime(dto.StartDate);
                    therapy.EndDate = dto.EndDate.HasValue ? DateOnly.FromDateTime(dto.EndDate.Value) : null;
                    therapy.DoctorId = dto.DoctorId;
                    therapy.UserId = dto.UserId;
                    therapy.UpdatedAt = DateTime.UtcNow;
                    _context.Therapies.Update(therapy);
                    await _context.SaveChangesAsync();

                    // MedicationSchedules: update, insert, delete
                    var existingSchedules = await _context.MedicationSchedules.Where(ms => ms.TherapyId == therapy.TherapyId).ToListAsync();
                    var dtoIds = dto.MedicationSchedules
                        .Where(ms => ms.MedicationScheduleId.HasValue && ms.MedicationScheduleId.Value > 0)
                        .Select(ms => ms.MedicationScheduleId.GetValueOrDefault())
                        .ToList();
                    // Elimina quelli rimossi
                    var toDelete = existingSchedules.Where(ms => !dtoIds.Contains(ms.MedicationScheduleId)).ToList();
                    if (toDelete.Any())
                    {
                        _context.MedicationSchedules.RemoveRange(toDelete);
                        await _context.SaveChangesAsync();
                    }
                    // Upsert
                    foreach (var msDto in dto.MedicationSchedules)
                    {
                        if (msDto.MedicationScheduleId.HasValue && msDto.MedicationScheduleId.Value > 0)
                        {
                            // Update
                            var ms = existingSchedules.FirstOrDefault(x => x.MedicationScheduleId == msDto.MedicationScheduleId.Value);
                            if (ms != null)
                            {
                                ms.MedicationName = msDto.MedicationName;
                                ms.ExpectedQuantity = msDto.ExpectedQuantity;
                                ms.ExpectedUnit = msDto.ExpectedUnit;
                                ms.ScheduledDateTime = msDto.ScheduledDateTime;
                                _context.MedicationSchedules.Update(ms);
                            }
                        }
                        else
                        {
                            // Insert
                            var ms = new Models.MedicationSchedules
                            {
                                TherapyId = therapy.TherapyId,
                                MedicationName = msDto.MedicationName,
                                ExpectedQuantity = msDto.ExpectedQuantity,
                                ExpectedUnit = msDto.ExpectedUnit,
                                ScheduledDateTime = msDto.ScheduledDateTime
                            };
                            _context.MedicationSchedules.Add(ms);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Insert nuova terapia
                    therapy = new Therapies
                    {
                        DoctorId = dto.DoctorId,
                        UserId = dto.UserId,
                        Instructions = dto.Instructions,
                        StartDate = DateOnly.FromDateTime(dto.StartDate),
                        EndDate = dto.EndDate.HasValue ? DateOnly.FromDateTime(dto.EndDate.Value) : null,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.Therapies.Add(therapy);
                    await _context.SaveChangesAsync();

                    // Insert medication schedules
                    foreach (var msDto in dto.MedicationSchedules)
                    {
                        var ms = new Models.MedicationSchedules
                        {
                            TherapyId = therapy.TherapyId,
                            MedicationName = msDto.MedicationName,
                            ExpectedQuantity = msDto.ExpectedQuantity,
                            ExpectedUnit = msDto.ExpectedUnit,
                            ScheduledDateTime = msDto.ScheduledDateTime
                        };
                        _context.MedicationSchedules.Add(ms);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Ok(isUpdate ? "Therapy and schedules updated successfully." : "Therapy and related schedules added successfully.");
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
                            ms.ScheduledDateTime
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (therapy == null)
                return NotFound("Therapy not found.");

            return Ok(therapy);
        }

        [HttpPut("therapy")]
        public async Task<IActionResult> UpdateTherapy([FromBody] TherapyWithSchedules therapyWithSchedules)
        {
            if (therapyWithSchedules == null || therapyWithSchedules.Therapy == null || therapyWithSchedules.Therapy.TherapyId <= 0)
                return BadRequest("Invalid therapy data.");

            var existingTherapy = await _context.Therapies
                .FirstOrDefaultAsync(t => t.TherapyId == therapyWithSchedules.Therapy.TherapyId);

            if (existingTherapy == null)
                return NotFound("Therapy not found.");

            try
            {
                existingTherapy.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
                _context.Therapies.Update(existingTherapy);

                var newTherapy = therapyWithSchedules.Therapy;
                newTherapy.TherapyId = 0;
                newTherapy.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
                _context.Therapies.Add(newTherapy);
                await _context.SaveChangesAsync();

                if (therapyWithSchedules.MedicationSchedules != null && therapyWithSchedules.MedicationSchedules.Any())
                {
                    foreach (var schedule in therapyWithSchedules.MedicationSchedules)
                    {
                        schedule.TherapyId = newTherapy.TherapyId;
                        _context.MedicationSchedules.Add(schedule);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok("Therapy updated successfully as a new therapy with related schedules.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("therapy")]
        public async Task<IActionResult> DeleteTherapy([FromQuery] int therapyId)
        {
            if (therapyId <= 0)
                return BadRequest("Invalid therapy ID.");

            var therapy = await _context.Therapies
                .FirstOrDefaultAsync(t => t.TherapyId == therapyId);

            if (therapy == null)
                return NotFound("Therapy not found.");

            try
            {
                therapy.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
                _context.Therapies.Update(therapy);
                await _context.SaveChangesAsync();
                return Ok("Therapy marked as inactive.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
