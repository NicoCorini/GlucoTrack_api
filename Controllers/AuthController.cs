using GlucoTrack_api.Models;
using GlucoTrack_api.Data;
using GlucoTrack_api.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // No real session management, just return 200 OK
            return Ok(new { message = "Logout successful" });
        }

    }
}
