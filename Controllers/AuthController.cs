using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PromptTrackerv1.Models;
using PromptTrackerAPI.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace PromptTrackerv1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            _context.Users.Add(user);
            _context.SaveChanges();
            return Ok(new { message = "User created" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User login)
        {
            var user = _context.Users.SingleOrDefault(u => u.Username == login.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.PasswordHash, user.PasswordHash))
                return Unauthorized();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("YourSuperSecretKey123!"); // Same key
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { token = tokenHandler.WriteToken(token) });
        }
    }
}
