using FloodLink.Models;

namespace FloodLink.Services
{
    public class UrgencyAnalysisResult
    {
        public string SuggestedUrgency { get; set; } = "Medium"; // "High", "Medium", "Low"
        public int Score { get; set; } // 0 - 100
        public List<string> TriggerReasons { get; set; } = new();
        public string Rationale => TriggerReasons.Count > 0 
            ? string.Join("; ", TriggerReasons) 
            : "Standard emergency assistance profile.";
    }

    public interface IAiUrgencyDetectionService
    {
        UrgencyAnalysisResult AnalyzeUrgency(string title, string description, int numberOfPeople, string category);
    }

    /// <summary>
    /// Section 4.13 — AI-Based Urgency Detection
    /// Analyzes help request descriptions for critical factors:
    /// Elderly people, Medical emergencies, Children, Rising water levels, Trapped victims, etc.
    /// Suggests High, Medium, or Low urgency while leaving the final decision to coordinators.
    /// </summary>
    public class AiUrgencyDetectionService : IAiUrgencyDetectionService
    {
        public UrgencyAnalysisResult AnalyzeUrgency(string title, string description, int numberOfPeople, string category)
        {
            var result = new UrgencyAnalysisResult();
            var text = $"{title} {description} {category}".ToLowerInvariant();

            int score = 20; // baseline

            // 1. Rising Water / Severe Flooding / Trapped (weight: +35)
            string[] waterTerms = { "rising water", "water rising", "roof", "trapped", "submerged", "drowning", 
                                   "water level increasing", "washed away", "boat urgently", "fast current", 
                                   "house surrounded", "cannot escape", "stranded", "neck deep" };
            var waterTriggers = waterTerms.Where(term => text.Contains(term)).ToList();
            if (waterTriggers.Any())
            {
                score += 35;
                result.TriggerReasons.Add("Critical water condition: " + string.Join(", ", waterTriggers.Take(2)));
            }

            // 2. Medical emergencies (weight: +40)
            string[] medicalTerms = { "medical", "heart", "oxygen", "bleeding", "unconscious", "injury", 
                                     "injured", "pregnant", "labor", "snake bite", "asthma", "seizure", 
                                     "diabetic", "critical patient", "no medicine", "dying" };
            var medicalTriggers = medicalTerms.Where(term => text.Contains(term)).ToList();
            if (medicalTriggers.Any())
            {
                score += 40;
                result.TriggerReasons.Add("Medical emergency detected: " + string.Join(", ", medicalTriggers.Take(2)));
            }

            // 3. Vulnerable demographics: Elderly, Children, Infants, Disabled (weight: +25)
            string[] vulnerableTerms = { "elderly", "old person", "old woman", "old man", "grandparent", 
                                        "child", "children", "baby", "infant", "newborn", "kid", "kids", 
                                        "disabled", "handicap", "paralyzed", "blind" };
            var vulnerableTriggers = vulnerableTerms.Where(term => text.Contains(term)).ToList();
            if (vulnerableTriggers.Any())
            {
                score += 25;
                result.TriggerReasons.Add("Vulnerable individuals present: " + string.Join(", ", vulnerableTriggers.Take(2)));
            }

            // 4. Food / Drinking Water Deprivation (weight: +20)
            string[] famineTerms = { "starving", "no food", "no drinking water", "no water for days", "hunger", "starvation" };
            var famineTriggers = famineTerms.Where(term => text.Contains(term)).ToList();
            if (famineTriggers.Any())
            {
                score += 20;
                result.TriggerReasons.Add("Severe food/water deprivation detected");
            }

            // 5. High head count
            if (numberOfPeople >= 10)
            {
                score += 15;
                result.TriggerReasons.Add($"Large group ({numberOfPeople} affected individuals)");
            }
            else if (numberOfPeople >= 5)
            {
                score += 10;
            }

            result.Score = Math.Min(100, score);

            if (result.Score >= 55)
            {
                result.SuggestedUrgency = "High";
            }
            else if (result.Score >= 35)
            {
                result.SuggestedUrgency = "Medium";
            }
            else
            {
                result.SuggestedUrgency = "Low";
            }

            return result;
        }
    }
}
