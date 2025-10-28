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

        // GET user prompts
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

        // Helper to validate & enrich a prompt from a DTO
        private (bool IsValid, string? ErrorMessage, Prompt? ProcessedPrompt) ValidateAndEnrichPrompt(PromptCreateDto dto, string username)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.InputText))
                return (false, "InputText is required.", null);

            if (dto.InputText.Length < 3 || dto.InputText.Length > 1000)
                return (false, "InputText must be between 3 and 1000 characters.", null);

            var prompt = new Prompt
            {
                InputText = dto.InputText,
                UserId = username,
                CreatedAt = DateTime.UtcNow
            };

            _enrichmentService.EnrichPrompt(prompt);

            return (true, null, prompt);
        }

        // POST single prompt using DTO
        [HttpPost]
        public IActionResult CreatePrompt([FromBody] PromptCreateDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var result = ValidateAndEnrichPrompt(dto, username);
            if (!result.IsValid)
                return BadRequest(new { message = result.ErrorMessage });

            try
            {
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
        public IActionResult CreateBatchPrompts([FromBody] List<PromptCreateDto> promptDtos)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            if (promptDtos == null || !promptDtos.Any())
                return BadRequest(new { message = "No prompts provided." });

            var results = new List<object>();

            foreach (var dto in promptDtos)
            {
                var result = ValidateAndEnrichPrompt(dto, username);

                if (!result.IsValid)
                {
                    results.Add(new { InputText = dto.InputText, Success = false, Errors = new[] { result.ErrorMessage } });
                    continue;
                }

                try
                {
                    _context.Prompts.Add(result.ProcessedPrompt!);
                    _context.SaveChanges();
                    results.Add(new
                    {
                        InputText = dto.InputText,
                        Success = true,
                        Id = result.ProcessedPrompt!.Id,
                        Category = result.ProcessedPrompt!.Category,
                        Source = result.ProcessedPrompt!.Source
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new { InputText = dto.InputText, Success = false, Errors = new[] { ex.Message } });
                }
            }

            return Ok(results);
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
