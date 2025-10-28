using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptTrackerAPI.Data;
using PromptTrackerv1.Models;
using PromptTrackerv1.Services;
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
        private readonly PromptEnrichmentService _enrichmentService;

        public PromptController(AppDbContext context, ILogger<PromptController> logger, PromptEnrichmentService enrichmentService)
        {
            _context = context;
            _logger = logger;
            _enrichmentService = enrichmentService;
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
                return StatusCode(500, new { message = "An error occurred while retrieving prompts." });
            }
        }

        // Helper to validate & enrich a prompt
        private (bool IsValid, string? ErrorMessage, Prompt? ProcessedPrompt) ValidateAndEnrichPrompt(Prompt prompt, string username)
        {
            if (prompt == null || string.IsNullOrWhiteSpace(prompt.InputText))
                return (false, "InputText is required.", null);

            if (prompt.InputText.Length < 3 || prompt.InputText.Length > 1000)
                return (false, "InputText must be between 3 and 1000 characters.", null);

            prompt.UserId = username;
            prompt.CreatedAt = DateTime.UtcNow;

            // enrich prompt
            prompt = _enrichmentService.EnrichPrompt(prompt);

            return (true, null, prompt);
        }

        // POST single prompt
        [HttpPost]
        public IActionResult CreatePrompt([FromBody] Prompt prompt)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Prompt creation failed validation: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var result = ValidateAndEnrichPrompt(prompt, username);
                if (!result.IsValid)
                    return BadRequest(new { message = result.ErrorMessage });

                _context.Prompts.Add(result.ProcessedPrompt!);
                _context.SaveChanges();

                _logger.LogInformation("Prompt successfully created by {User}.", username);
                return CreatedAtAction(nameof(GetUserPrompts), new { id = result.ProcessedPrompt!.Id }, result.ProcessedPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prompt.");
                return StatusCode(500, new { message = "An error occurred while saving the prompt." });
            }
        }

        // POST batch of prompts
        [HttpPost("batch")]
        public IActionResult CreatePromptsBatch([FromBody] List<Prompt> prompts)
        {
            if (prompts == null || !prompts.Any())
                return BadRequest(new { message = "No prompts provided." });

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var results = new List<object>();

            foreach (var prompt in prompts)
            {
                try
                {
                    var result = ValidateAndEnrichPrompt(prompt, username);
                    if (!result.IsValid)
                    {
                        results.Add(new { prompt = prompt.InputText, success = false, error = result.ErrorMessage });
                        continue;
                    }

                    _context.Prompts.Add(result.ProcessedPrompt!);
                    _context.SaveChanges();

                    results.Add(new
                    {
                        prompt = prompt.InputText,
                        success = true,
                        id = result.ProcessedPrompt!.Id,
                        category = result.ProcessedPrompt!.Category,
                        source = result.ProcessedPrompt!.Source
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating prompt: {Prompt}", prompt.InputText);
                    results.Add(new { prompt = prompt.InputText, success = false, error = ex.Message });
                }
            }

            return Ok(new { results });
        }

        // DELETE -- for admins
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

                _logger.LogInformation("Prompt with ID {Id} deleted successfully.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting prompt with ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the prompt." });
            }
        }
    }
}
