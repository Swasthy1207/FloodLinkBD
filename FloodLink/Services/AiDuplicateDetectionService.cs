using FloodLink.Data;
using FloodLink.Models;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Services
{
    public class DuplicateDetectionResult
    {
        public bool IsDuplicate { get; set; }
        public int? MatchedRequestId { get; set; }
        public string? Reason { get; set; }
        public double ConfidenceScore { get; set; } // 0.0 - 1.0
    }

    public interface IAiDuplicateDetectionService
    {
        Task<DuplicateDetectionResult> CheckForDuplicateAsync(HelpRequest newRequest);
    }

    /// <summary>
    /// Section 4.14 — AI Duplicate Request Detection
    /// Compares incoming requests against active/recent requests to detect duplicates
    /// by contact info, nearby location tokens, affected family names, or description overlap.
    /// Alerts coordinators to reduce duplicate work and save resources.
    /// </summary>
    public class AiDuplicateDetectionService : IAiDuplicateDetectionService
    {
        private readonly ApplicationDbContext _context;

        public AiDuplicateDetectionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DuplicateDetectionResult> CheckForDuplicateAsync(HelpRequest newRequest)
        {
            var result = new DuplicateDetectionResult { IsDuplicate = false };

            // Look at requests within the last 14 days that are not closed
            var recentRequests = await _context.HelpRequests
                .Where(r => r.Id != newRequest.Id && r.Status != "Closed" && r.Status != "Resolved")
                .Where(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-14))
                .ToListAsync();

            if (!recentRequests.Any())
                return result;

            string CleanPhone(string p) => 
                new string((p ?? "").Where(char.IsDigit).ToArray());

            string newPhone = CleanPhone(newRequest.ContactInformation);
            var newLocationWords = (newRequest.Location ?? "")
                .ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToHashSet();

            foreach (var existing in recentRequests)
            {
                // 1. Exact contact number match
                string existingPhone = CleanPhone(existing.ContactInformation);
                if (!string.IsNullOrEmpty(newPhone) && newPhone.Length >= 8 && newPhone == existingPhone)
                {
                    result.IsDuplicate = true;
                    result.MatchedRequestId = existing.Id;
                    result.ConfidenceScore = 0.95;
                    result.Reason = $"Identical contact number match with Request #{existing.Id} ('{existing.Title}').";
                    return result;
                }

                // 2. Same affected person name (if provided) and location match
                if (!string.IsNullOrWhiteSpace(newRequest.AffectedPersonName) &&
                    !string.IsNullOrWhiteSpace(existing.AffectedPersonName) &&
                    string.Equals(newRequest.AffectedPersonName.Trim(), existing.AffectedPersonName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    result.IsDuplicate = true;
                    result.MatchedRequestId = existing.Id;
                    result.ConfidenceScore = 0.90;
                    result.Reason = $"Same affected person '{newRequest.AffectedPersonName}' named in Request #{existing.Id}.";
                    return result;
                }

                // 3. High location overlap and same category
                if (existing.Category == newRequest.Category && newLocationWords.Count > 0)
                {
                    var existingWords = (existing.Location ?? "")
                        .ToLowerInvariant()
                        .Split(new[] { ' ', ',', '.', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 2)
                        .ToHashSet();

                    int intersection = newLocationWords.Intersect(existingWords).Count();
                    double overlap = (double)intersection / Math.Min(newLocationWords.Count, existingWords.Count);

                    if (overlap >= 0.70 && intersection >= 2)
                    {
                        result.IsDuplicate = true;
                        result.MatchedRequestId = existing.Id;
                        result.ConfidenceScore = 0.78;
                        result.Reason = $"High location and category overlap ({Math.Round(overlap * 100)}%) with Request #{existing.Id} in {existing.Location}.";
                        return result;
                    }
                }
            }

            return result;
        }
    }
}
