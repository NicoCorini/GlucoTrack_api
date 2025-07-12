
using GlucoTrack_api.Models;
using GlucoTrack_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController : Controller
    {
        private readonly GlucoTrackDBContext _context;
        public AdminController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns all users except those with the 'admin' role.
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<Users>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new Users
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Username = u.Username,
                    PasswordHash = u.PasswordHash,
                    BirthDate = u.BirthDate,
                    Height = u.Height,
                    Weight = u.Weight,
                    FiscalCode = u.FiscalCode,
                    Gender = u.Gender,
                    Specialization = u.Specialization,
                    AffiliatedHospital = u.AffiliatedHospital,
                    RoleId = u.RoleId,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Adds a new user or updates an existing user. If UserId is provided and valid, updates the user; otherwise, creates a new user.
        /// </summary>
        [HttpPost("user")]
        public async Task<IActionResult> AddOrUpdateUser([FromBody] DTOs.UserDto userDto)
        {
            if (userDto == null)
                return BadRequest("Invalid user data.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool isUpdate = userDto.UserId > 0;
                if (isUpdate)
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userDto.UserId);
                    if (existingUser == null)
                        return NotFound("User not found.");

                    // Update fields
                    existingUser.FirstName = userDto.FirstName;
                    existingUser.LastName = userDto.LastName;
                    existingUser.Email = userDto.Email;
                    existingUser.Username = userDto.Username;
                    existingUser.BirthDate = userDto.BirthDate;
                    existingUser.Height = userDto.Height;
                    existingUser.Weight = userDto.Weight;
                    existingUser.FiscalCode = userDto.FiscalCode;
                    existingUser.Gender = userDto.Gender;
                    existingUser.Specialization = userDto.Specialization;
                    existingUser.AffiliatedHospital = userDto.AffiliatedHospital;
                    existingUser.RoleId = userDto.RoleId;
                    existingUser.PasswordHash = userDto.PasswordHash;

                    _context.Users.Update(existingUser);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // New user
                    var newUser = new Users
                    {
                        Username = userDto.Username,
                        PasswordHash = userDto.PasswordHash,
                        FirstName = userDto.FirstName,
                        LastName = userDto.LastName,
                        Email = userDto.Email,
                        RoleId = userDto.RoleId,
                        BirthDate = userDto.BirthDate,
                        Height = userDto.Height,
                        Weight = userDto.Weight,
                        FiscalCode = userDto.FiscalCode,
                        Gender = userDto.Gender,
                        Specialization = userDto.Specialization,
                        AffiliatedHospital = userDto.AffiliatedHospital,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Ok("User saved successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a user by Id. Returns 200 OK on success, 404 if not found, 400 for invalid id.
        /// </summary>
        [HttpDelete("user")]
        public async Task<IActionResult> DeleteUser([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid userId.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound("User not found.");

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok("User deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
