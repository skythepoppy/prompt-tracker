using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptTrackerAPI.Data;
using PromptTrackerv1.Models;
using System.Security.Claims;

namespace PromptTrackerv1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PromptController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PromptController> _logger;

        public PromptController(AppDbContext context, ILogger<PromptController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET
        [HttpGet]
        public IActionResult GetUserPrompts()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { message = "Invalid or missing user identity." });

                var prompts = _context.Prompts
                    .Where(p => p.UserId == username)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();

                return Ok(prompts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prompts.");
                return StatusCode(500, "An error occurred while retrieving prompts.");
            }
        }

        // POST
        [HttpPost]
        public IActionResult CreatePrompt([FromBody] Prompt prompt)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                prompt.UserId = username;
                prompt.CreatedAt = DateTime.UtcNow;

                _context.Prompts.Add(prompt);
                _context.SaveChanges();

                return CreatedAtAction(nameof(GetUserPrompts), new { id = prompt.Id }, prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prompt.");
                return StatusCode(500, "An error occurred while saving the prompt.");
            }
        }

        // DELETE (admins only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public IActionResult DeletePrompt(int id)
        {
            try
            {
                var prompt = _context.Prompts.Find(id);
                if (prompt == null)
                    return NotFound(new { message = "Prompt not found." });

                _context.Prompts.Remove(prompt);
                _context.SaveChanges();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting prompt with ID {Id}", id);
                return StatusCode(500, "An error occurred while deleting the prompt.");
            }
        }
    }
}
