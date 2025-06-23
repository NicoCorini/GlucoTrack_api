using GlucoTrack_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly GlucoTrackDBContext _context;

        public AuthController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        public class LoginRequest
        {
            [Required]
            public string EmailOrUsername { get; set; }

            [Required]
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.TabUtenti
                .FirstOrDefaultAsync(u =>
                    u.Email == request.EmailOrUsername ||
                    u.NomeUtente == request.EmailOrUsername);

            if (user == null || user.HashPassword != request.Password)
                return Unauthorized("Invalid credentials");

            // Compose simplified response
            var response = new
            {
                user.IdUtente,
                user.NomeUtente,
                user.Nome,
                user.Cognome,
                user.Email,
                user.IdRuolo,
                user.CreatoIl,
                user.UltimoAccesso
            };

            // Optionally update last access
            user.UltimoAccesso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(response);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // No real session management, just return 200 OK
            return Ok(new { message = "Logout successful" });
        }

    }
}
