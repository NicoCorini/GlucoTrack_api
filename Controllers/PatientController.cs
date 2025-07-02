using GlucoTrack_api.Models;
using GlucoTrack_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlucoTrack_api.DTOs.Patient;
using System.Globalization;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PatientController : Controller
    {
        private readonly GlucoTrackDBContext _context;

        public PatientController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        [HttpGet("info")]
        public async Task<ActionResult<PatientInfoResponseDto>> GetPatientInfo([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid user ID.");

            // Carica utente con navigation properties corrette
            var nowDateOnly = DateOnly.FromDateTime(DateTime.Now);
            var user = await _context.Users
                .Include(u => u.PatientDoctorsPatient.Where(pd => pd.EndDate == null))
                    .ThenInclude(pd => pd.Doctor)
                .Include(u => u.PatientRiskFactors)
                    .ThenInclude(prf => prf.RiskFactor)
                .Include(u => u.PatientComorbidities)
                .Include(u => u.TherapiesUser.Where(t => !t.EndDate.HasValue || t.EndDate > nowDateOnly))
                    .ThenInclude(t => t.MedicationSchedules)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            // Medico attuale
            var currentDoctor = user.PatientDoctorsPatient.FirstOrDefault()?.Doctor;

            // Fattori rischio
            var riskFactors = user.PatientRiskFactors
                .Select(prf => new RiskFactorDto
                {
                    RiskFactorId = prf.RiskFactor.RiskFactorId,
                    Label = prf.RiskFactor.Label,
                    Description = prf.RiskFactor.Description
                }).ToList();

            // Comorbidità (solo stringa, come da model)
            var comorbidities = user.PatientComorbidities
                .Select(pc => new GlucoTrack_api.DTOs.Patient.ComorbidityDto
                {
                    Comorbidity = pc.Comorbidity ?? string.Empty,
                    StartDate = pc.StartDate,
                    EndDate = pc.EndDate
                }).ToList();

            // Terapie attive con medication schedule
            var therapies = user.TherapiesUser
                .Select(t => new TherapyWithSchedulesResponseDto
                {
                    TherapyId = t.TherapyId,
                    Instructions = t.Instructions,
                    StartDate = t.StartDate?.ToDateTime(TimeOnly.MinValue),
                    EndDate = t.EndDate?.ToDateTime(TimeOnly.MinValue),
                    MedicationSchedules = t.MedicationSchedules.Select(ms => new MedicationScheduleDto
                    {
                        MedicationName = ms.MedicationName,
                        ExpectedQuantity = (double)ms.ExpectedQuantity,
                        ExpectedUnit = ms.ExpectedUnit,
                        ScheduledDateTime = ms.ScheduledDateTime
                    }).ToList()
                }).ToList();

            var response = new PatientInfoResponseDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                BirthDate = user.BirthDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                Height = user.Height,
                Weight = user.Weight,
                FiscalCode = user.FiscalCode ?? string.Empty,
                Gender = user.Gender ?? string.Empty,
                CurrentDoctor = currentDoctor == null ? null : new DoctorInfoDto
                {
                    DoctorId = currentDoctor.UserId,
                    FirstName = currentDoctor.FirstName ?? string.Empty,
                    LastName = currentDoctor.LastName ?? string.Empty,
                    Email = currentDoctor.Email ?? string.Empty,
                    Specialization = currentDoctor.Specialization ?? string.Empty,
                    AffiliatedHospital = currentDoctor.AffiliatedHospital ?? string.Empty
                },
                RiskFactors = riskFactors,
                Comorbidities = comorbidities,
                Therapies = therapies
            };
            return Ok(response);
        }

        [HttpGet("glycemic-resume")]
        public async Task<ActionResult<List<GlycemicResumeResponseDto>>> GetGlycemicResume([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid user ID.");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound("User not found.");
            var measurements = await _context.GlycemicMeasurements.Where(m => m.UserId == userId).ToListAsync();
            string[] weekDays = { "sun", "mon", "tue", "wed", "thu", "fri", "sat" };
            DateTime today = DateTime.Today;
            DateTime sevenDaysAgo = today.AddDays(-6);
            var result = new List<GlycemicResumeResponseDto>();
            for (int i = 0; i < 7; i++)
            {
                var day = sevenDaysAgo.AddDays(i);
                var dayMeasurements = measurements.Where(m => m.MeasurementDateTime.Date == day.Date).ToList();
                double avg = dayMeasurements.Any() ? dayMeasurements.Average(m => m.Value) : 0;
                string dayLabel = weekDays[(int)day.DayOfWeek];
                result.Add(new GlycemicResumeResponseDto { Day = dayLabel, Average = Math.Round(avg, 2) });
            }
            return Ok(result);
        }

        [HttpGet("daily-resume")]
        public async Task<ActionResult<DailyResumeResponseDto>> GetDailyResume([FromQuery] int userId, [FromQuery] DateOnly date)
        {
            if (userId <= 0 || date == default)
                return BadRequest("Invalid parameters.");
            var glycemicMeasurements = await _context.GlycemicMeasurements
                .Where(r => r.UserId == userId && DateOnly.FromDateTime(r.MeasurementDateTime.Date) == date)
                .ToListAsync();
            var medicationIntakes = await _context.MedicationIntakes
                .Where(a => a.UserId == userId && DateOnly.FromDateTime(a.IntakeDateTime.Date) == date)
                .ToListAsync();
            var symptoms = await _context.Symptoms
                .Where(s => s.UserId == userId && DateOnly.FromDateTime(s.OccurredAt) == date)
                .ToListAsync();
            var response = new DailyResumeResponseDto
            {
                GlycemicMeasurements = glycemicMeasurements,
                MedicationIntakes = medicationIntakes,
                Symptoms = symptoms
            };
            return Ok(response);
        }

        [HttpGet("glycemic-measurement")]
        public async Task<ActionResult<GlycemicMeasurements>> GetGlycemicMeasurement([FromQuery] int glycemicMeasurementId)
        {
            if (glycemicMeasurementId <= 0)
                return BadRequest("Invalid glycemic measurement ID.");

            var entity = await _context.GlycemicMeasurements.FirstOrDefaultAsync(g => g.GlycemicMeasurementId == glycemicMeasurementId);

            if (entity == null)
                return NotFound("Glycemic measurement not found.");

            return Ok(entity);
        }

        [HttpPost("add-glycemic-log")]
        public async Task<ActionResult> AddOrUpdateGlycemicLog([FromBody] AddGlycemicLogRequestDto glycemicLog)
        {
            if (glycemicLog == null || glycemicLog.UserId <= 0 || glycemicLog.Value <= 0)
                return BadRequest("Invalid glycemic log data.");
            try
            {
                if (glycemicLog.GlycemicMeasurementId > 0)
                {
                    // UPDATE
                    var entity = await _context.GlycemicMeasurements
                        .FirstOrDefaultAsync(g => g.GlycemicMeasurementId == glycemicLog.GlycemicMeasurementId);
                    if (entity == null)
                        return NotFound("Glycemic measurement not found.");

                    entity.MeasurementDateTime = glycemicLog.MeasurementDateTime;
                    entity.Value = (short)glycemicLog.Value;
                    entity.Note = glycemicLog.Note;
                    entity.MeasurementTypeId = glycemicLog.MeasurementTypeId;
                    entity.MealTypeId = glycemicLog.MealTypeId;

                    await _context.SaveChangesAsync();
                    return Ok("Glycemic log updated successfully.");
                }
                else
                {
                    // INSERT
                    var entity = new GlycemicMeasurements
                    {
                        UserId = glycemicLog.UserId,
                        MeasurementDateTime = glycemicLog.MeasurementDateTime,
                        Value = (short)glycemicLog.Value,
                        Note = glycemicLog.Note,
                        MeasurementTypeId = glycemicLog.MeasurementTypeId,
                        MealTypeId = glycemicLog.MealTypeId
                    };
                    _context.GlycemicMeasurements.Add(entity);
                    await _context.SaveChangesAsync();
                    return Ok("Glycemic log added successfully.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("delete-glycemic-measurement")]
        public async Task<IActionResult> DeleteGlycemicMeasurement([FromQuery] int glycemicMeasurementId)
        {
            if (glycemicMeasurementId <= 0)
                return BadRequest("Invalid glycemic measurement ID.");

            var entity = await _context.GlycemicMeasurements.FirstOrDefaultAsync(g => g.GlycemicMeasurementId == glycemicMeasurementId);
            if (entity == null)
                return NotFound("Glycemic measurement not found.");

            _context.GlycemicMeasurements.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok("Glycemic measurement deleted successfully.");
        }

        [HttpGet("symptom-log")]
        public async Task<ActionResult<GlycemicMeasurements>> GetSymtptomLog([FromQuery] int symptomId)
        {
            if (symptomId <= 0)
                return BadRequest("Invalid symptom log ID.");

            var entity = await _context.Symptoms.FirstOrDefaultAsync(s => s.SymptomId == symptomId);

            if (entity == null)
                return NotFound("Symptom log not found.");

            return Ok(entity);
        }

        [HttpPost("add-symptom-log")]
        public async Task<ActionResult> AddOrUpdateSymptomLog([FromBody] AddSymptomLogRequestDto symptomLog)
        {
            if (symptomLog == null || symptomLog.UserId <= 0 || string.IsNullOrEmpty(symptomLog.Description))
                return BadRequest("Invalid symptom log data.");
            try
            {
                if (symptomLog.SymptomId > 0)
                {
                    // Update
                    var entity = await _context.Symptoms.FirstOrDefaultAsync(s => s.SymptomId == symptomLog.SymptomId);
                    if (entity == null)
                        return NotFound("Symptom log not found.");
                    entity.Description = symptomLog.Description;
                    entity.OccurredAt = symptomLog.OccurredAt;
                    await _context.SaveChangesAsync();
                    return Ok("Symptom log updated successfully.");
                }
                else
                {
                    // Insert
                    var entity = new Symptoms
                    {
                        UserId = symptomLog.UserId,
                        Description = symptomLog.Description,
                        OccurredAt = symptomLog.OccurredAt
                    };
                    _context.Symptoms.Add(entity);
                    await _context.SaveChangesAsync();
                    return Ok("Symptom log added successfully.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("delete-symptom-log")]
        public async Task<IActionResult> DeleteSymptomLog([FromQuery] int symptomId)
        {
            if (symptomId <= 0)
                return BadRequest("Invalid symptom log ID.");
            var entity = await _context.Symptoms.FirstOrDefaultAsync(s => s.SymptomId == symptomId);
            if (entity == null)
                return NotFound("Symptom log not found.");
            _context.Symptoms.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok("Symptom log deleted successfully.");
        }

        [HttpPost("add-medication-log")]
        public async Task<ActionResult> AddMedicationLog([FromBody] AddMedicationLogRequestDto medicationLog)
        {
            if (medicationLog == null || medicationLog.UserId <= 0)
                return BadRequest("Invalid medication log data.");
            try
            {
                var entity = new MedicationIntakes
                {
                    UserId = medicationLog.UserId,
                    IntakeDateTime = medicationLog.IntakeDateTime,
                    ExpectedQuantityValue = (decimal)medicationLog.ExpectedQuantityValue,
                    Unit = medicationLog.Unit ?? string.Empty,
                    Note = medicationLog.Note,
                    MedicationTakenName = medicationLog.MedicationTakenName,
                    MedicationScheduleId = medicationLog.MedicationScheduleId
                };
                _context.MedicationIntakes.Add(entity);
                await _context.SaveChangesAsync();
                return Ok("Medication log added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("therapies")]
        public async Task<ActionResult<List<TherapyWithSchedulesResponseDto>>> GetTherapiesWithSchedules([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid patient ID.");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound("Patient not found.");
            var therapies = await _context.Therapies
                .Where(t => t.UserId == userId)
                .Select(t => new TherapyWithSchedulesResponseDto
                {
                    TherapyId = t.TherapyId,
                    Instructions = t.Instructions,
                    StartDate = t.StartDate.HasValue ? t.StartDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    EndDate = t.EndDate.HasValue ? t.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    MedicationSchedules = _context.MedicationSchedules
                        .Where(ms => ms.TherapyId == t.TherapyId)
                        .Select(ms => new MedicationScheduleDto
                        {
                            MedicationName = ms.MedicationName,
                            ExpectedQuantity = (double)ms.ExpectedQuantity,
                            ExpectedUnit = ms.ExpectedUnit,
                            ScheduledDateTime = ms.ScheduledDateTime
                        }).ToList()
                }).ToListAsync();
            if (therapies == null || !therapies.Any())
                return NotFound("No therapies found for this patient.");
            return Ok(therapies);
        }




    }
}
