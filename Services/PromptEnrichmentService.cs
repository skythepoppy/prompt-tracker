using System.Text.RegularExpressions;
using PromptTrackerv1.Models;

namespace PromptTrackerv1.Services
{
    public class PromptEnrichmentService
    {
        public (string Category, string Source) EnrichPrompt(string inputText, string? responseText = null)
        {
            if (string.IsNullOrWhiteSpace(inputText))
                return ("General", "Unknown");

            string lowerInput = inputText.ToLower();

            // rule based classification ("pythonic c#)
            if (Regex.IsMatch(lowerInput, @"\b(code|program|bug|algorithm|api|c#|python|java)\b"))
                return ("Coding", "User");
            else if (Regex.IsMatch(lowerInput, @"\b(write|essay|story|paragraph|poem)\b"))
                return ("Writing", "User");
            else if (Regex.IsMatch(lowerInput, @"\b(math|equation|calculate|solve|formula)\b"))
                return ("Math", "User");
            else if (Regex.IsMatch(lowerInput, @"\b(data|analyze|statistics|ai|ml|training)\b"))
                return ("AI/Analytics", "System");
            else
                return ("General", "User");
        }
    }
}
