using GlucoTrack_api.Models;
using GlucoTrack_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var recentTherapies = await _context.Therapies
                .Where(t => t.DoctorId == doctorId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

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
        public async Task<IActionResult> AddTherapy([FromBody] TherapyWithSchedules therapyWithSchedules)
        {
            if (therapyWithSchedules == null || therapyWithSchedules.Therapy == null)
                return BadRequest("Invalid therapy data.");

            try
            {
                var therapy = therapyWithSchedules.Therapy;
                _context.Therapies.Add(therapy);
                await _context.SaveChangesAsync();

                if (therapyWithSchedules.MedicationSchedules != null && therapyWithSchedules.MedicationSchedules.Any())
                {
                    foreach (var schedule in therapyWithSchedules.MedicationSchedules)
                    {
                        schedule.TherapyId = therapy.TherapyId;
                        _context.MedicationSchedules.Add(schedule);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok("Therapy and related schedules added successfully.");
            }
            catch (Exception ex)
            {
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
