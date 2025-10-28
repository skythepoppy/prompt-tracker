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

        // GET: api/prompt
        [HttpGet]
        public IActionResult GetUserPrompts()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new ApiResponse<string>(false, "Invalid or missing user identity."));

                var prompts = _context.Prompts
                    .Where(p => p.UserId == username)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();

                if (prompts == null || !prompts.Any())
                    return Ok(new ApiResponse<List<Prompt>>(true, "No prompts found for this user.", new List<Prompt>()));

                return Ok(new ApiResponse<List<Prompt>>(true, "Prompts retrieved successfully.", prompts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prompts for user.");
                return StatusCode(500, new ApiResponse<string>(false, "An error occurred while retrieving prompts."));
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
            if (dto == null || string.IsNullOrWhiteSpace(dto.InputText))
                return BadRequest(ApiResponse<object>.Fail("InputText is required."));

            if (dto.InputText.Length < 3 || dto.InputText.Length > 1000)
                return BadRequest(ApiResponse<object>.Fail("InputText must be between 3 and 1000 characters."));

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized(ApiResponse<object>.Fail("Unauthorized user."));

            var prompt = new Prompt
            {
                InputText = dto.InputText,
                UserId = username,
                CreatedAt = DateTime.UtcNow
            };

            _enrichmentService.EnrichPrompt(prompt);

            try
            {
                _context.Prompts.Add(prompt);
                _context.SaveChanges();

                return Ok(ApiResponse<Prompt>.Ok("Prompt successfully created.", prompt));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prompt.");
                return StatusCode(500, ApiResponse<object>.Fail($"An error occurred while saving the prompt: {ex.Message}"));

            }
        }


        // POST batch of prompts
        [HttpPost("batch")]
        public IActionResult CreateBatchPrompts([FromBody] List<PromptCreateDto> promptDtos)
        {
            if (promptDtos == null || !promptDtos.Any())
                return BadRequest(ApiResponse<object>.Fail("No prompts provided."));

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized(ApiResponse<object>.Fail("Unauthorized user."));

            var results = new List<object>();

            foreach (var dto in promptDtos)
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(dto.InputText))
                    errors.Add("InputText is required.");
                else if (dto.InputText.Length < 3 || dto.InputText.Length > 1000)
                    errors.Add("InputText must be between 3 and 1000 characters.");

                if (errors.Any())
                {
                    results.Add(new { InputText = dto.InputText, Success = false, Errors = errors });
                    continue;
                }

                var prompt = new Prompt
                {
                    InputText = dto.InputText,
                    UserId = username,
                    CreatedAt = DateTime.UtcNow
                };

                _enrichmentService.EnrichPrompt(prompt);

                try
                {
                    _context.Prompts.Add(prompt);
                    _context.SaveChanges();
                    results.Add(new
                    {
                        InputText = dto.InputText,
                        Success = true,
                        Id = prompt.Id,
                        Category = prompt.Category,
                        Source = prompt.Source
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new { InputText = dto.InputText, Success = false, Errors = new[] { ex.Message } });
                }
            }

            return Ok(ApiResponse<object>.Ok("Batch processed successfully.", results));

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
