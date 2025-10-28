using System.Text.RegularExpressions;
using PromptTrackerv1.Models;

namespace PromptTrackerv1.Services
{
    public class PromptEnrichmentService
    {
        public Prompt EnrichPrompt(Prompt prompt)
        {
            if (prompt == null)
                throw new ArgumentNullException(nameof(prompt));

            string lowerInput = prompt.InputText.ToLower();

            // Rule-based classification
            if (Regex.IsMatch(lowerInput, @"\b(code|program|bug|algorithm|api|c#|python|java)\b"))
                prompt.Category = "Coding";
            else if (Regex.IsMatch(lowerInput, @"\b(write|essay|story|paragraph|poem)\b"))
                prompt.Category = "Writing";
            else if (Regex.IsMatch(lowerInput, @"\b(math|equation|calculate|solve|formula)\b"))
                prompt.Category = "Math";
            else if (Regex.IsMatch(lowerInput, @"\b(data|analyze|statistics|ai|ml|training)\b"))
                prompt.Category = "AI/Analytics";
            else
                prompt.Category = "General";

            // Optional: Set Source if empty
            if (string.IsNullOrEmpty(prompt.Source))
            {
                prompt.Source = "User";
            }

            return prompt;
        }
    }
}
