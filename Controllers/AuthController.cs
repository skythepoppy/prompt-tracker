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
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, IConfiguration config, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (_context.Users.Any(u => u.Username == user.Username))
                    return Conflict(new { message = "Username already exists." });

                if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash.Length < 6)
                    return BadRequest(new { message = "Password must be at least 6 characters long." });

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                user.Role = string.IsNullOrEmpty(user.Role) ? "User" : user.Role;

                _context.Users.Add(user);
                _context.SaveChanges();

                _logger.LogInformation("New user '{Username}' registered successfully.", user.Username);
                return Ok(new { message = "User created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration.");
                return StatusCode(500, new { message = "An error occurred while creating the user." });
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User login)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = _context.Users.SingleOrDefault(u => u.Username == login.Username);
                if (user == null || !BCrypt.Net.BCrypt.Verify(login.PasswordHash, user.PasswordHash))
                    return Unauthorized(new { message = "Invalid username or password." });

                var jwtSettings = _config.GetSection("Jwt");
                var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key missing in config."));
                var issuer = jwtSettings["Issuer"];
                var audience = jwtSettings["Audience"];

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role)
                    }),
                    Expires = DateTime.UtcNow.AddHours(2),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                    )
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwt = tokenHandler.WriteToken(token);

                _logger.LogInformation("User '{Username}' logged in successfully.", user.Username);
                return Ok(new { token = jwt, username = user.Username, role = user.Role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user '{Username}'.", login.Username);
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }
    }
}
