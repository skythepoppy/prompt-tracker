using Microsoft.AspNetCore.Mvc;
using PromptTrackerAPI.Data;
using PromptTrackerv1.Models;

namespace PromptTrackerv1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PromptController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetPrompts()
        {
            var prompts = _context.Prompts.ToList();
            return Ok(prompts);
        }

        [HttpPost]
        public IActionResult CreatePrompt(Prompt prompt)
        {
            _context.Prompts.Add(prompt);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetPrompts), new { id = prompt.Id }, prompt);
        }
    }
}
