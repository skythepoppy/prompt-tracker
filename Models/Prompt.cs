using System;
using System.ComponentModel.DataAnnotations;

namespace PromptTrackerv1.Models
{
    public class Prompt
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "UserId cannot exceed 100 characters.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "InputText is required.")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "InputText must be between 3 and 1000 characters.")]
        public string InputText { get; set; } = string.Empty;

        public string? ResponseText { get; set; }

        [StringLength(50, ErrorMessage = "Source cannot exceed 50 characters.")]
        public string Source { get; set; } = "manual"; // enrichment 

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
