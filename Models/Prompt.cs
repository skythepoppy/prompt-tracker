namespace PromptTrackerv1.Models
{
    public class Prompt
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;
        public string? ResponseText { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}