using GlucoTrack_api.Data;
using GlucoTrack_api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly GlucoTrackDBContext _context;

        public AuthController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Authenticates a user by email or username and password. Updates last access on success.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == request.EmailOrUsername ||
                    u.Username == request.EmailOrUsername);

            if (user == null || user.PasswordHash != request.Password)
                return Unauthorized("Invalid credentials");

            // Optionally update last access
            user.LastAccess = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new LoginResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                LastAccess = user.LastAccess ?? DateTime.UtcNow,
                RoleId = user.RoleId
            });

        }

        /// <summary>
        /// Logs out the user. No real session management; always returns 200 OK.
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // No real session management, just return 200 OK
            return Ok(new { message = "Logout successful" });
        }


        /// <summary>
        /// Returns detailed user information, including demographics, current doctor, risk factors, comorbidities, and active therapies with medication schedules.
        /// </summary>
        [HttpGet("info")]
        public async Task<ActionResult<UserInfoResponseDto>> GetUserInfo([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid user ID.");

            // Load user with correct navigation properties
            var nowDateOnly = DateOnly.FromDateTime(DateTime.Now);
            var user = await _context.Users
                .Include(u => u.PatientDoctorsPatient.Where(pd => pd.EndDate == null))
                    .ThenInclude(pd => pd.Doctor)
                .Include(u => u.PatientRiskFactors)
                    .ThenInclude(prf => prf.RiskFactor)
                .Include(u => u.ClinicalComorbidities)
                .Include(u => u.TherapiesUser)
                    .ThenInclude(t => t.MedicationSchedules)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            // Current doctor
            var currentDoctor = user.PatientDoctorsPatient.FirstOrDefault()?.Doctor;

            // Risk factors
            var riskFactors = user.PatientRiskFactors
                .Select(prf => new RiskFactorDto
                {
                    RiskFactorId = prf.RiskFactor.RiskFactorId,
                    Label = prf.RiskFactor.Label,
                    Description = prf.RiskFactor.Description
                }).ToList();

            // Comorbidities (string only, as per model)
            var comorbidities = user.ClinicalComorbidities
                .Select(pc => new ComorbidityDto
                {
                    Comorbidity = pc.Comorbidity ?? string.Empty,
                    StartDate = pc.StartDate,
                    EndDate = pc.EndDate
                }).ToList();

            // Active therapies with medication schedules
            var therapies = user.TherapiesUser
                .Select(t => new TherapyWithSchedulesResponseDto
                {
                    TherapyId = t.TherapyId,
                    Title = t.Title ?? string.Empty,
                    Instructions = t.Instructions ?? string.Empty,
                    StartDate = t.StartDate?.ToDateTime(TimeOnly.MinValue),
                    EndDate = t.EndDate?.ToDateTime(TimeOnly.MinValue),
                    MedicationSchedules = t.MedicationSchedules.Select(ms => new MedicationScheduleDto
                    {
                        MedicationName = ms.MedicationName,
                        Quantity = ms.Quantity,
                        Unit = ms.Unit,
                        DailyIntakes = ms.DailyIntakes
                    }).ToList()
                }).ToList();

            var response = new UserInfoResponseDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                RoleId = user.RoleId,
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
                Therapies = therapies,
                Specialization = user.Specialization ?? string.Empty,
                AffiliatedHospital = user.AffiliatedHospital ?? string.Empty
            };
            return Ok(response);
        }

    }
}
