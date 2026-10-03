using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Suggests events based on the user's search history.
    /// Scoring: category frequency x3, keyword matches x2, plus a priority bonus.
    /// A max-first MinPriorityQueue picks the best-scoring events.
    /// </summary>
    public static class RecommendationService
    {
        private const int CategoryWeight = 3;
        private const int KeywordWeight = 2;

        /// <param name="count">Maximum number of recommendations.</param>
        /// <param name="exclude">Optional set of event IDs to leave out (e.g. already on screen).</param>
        public static List<EventRecommendation> GetRecommendations(int count, HashSet<Guid> exclude = null)
        {
            var repo = EventRepository.Instance;
            var history = SearchHistoryService.Instance;

            var upcoming = repo.GetAllEvents()
                               .Where(e => e.Date.Date >= DateTime.Today)
                               .Where(e => exclude == null || !exclude.Contains(e.Id))
                               .ToList();

            // Highest score first; ties broken by earliest date
            var queue = new MinPriorityQueue<EventRecommendation>((a, b) =>
            {
                int byScore = b.Score.CompareTo(a.Score);
                return byScore != 0 ? byScore : a.Event.Date.CompareTo(b.Event.Date);
            });

            foreach (var evt in upcoming)
            {
                int score = 0;
                string reason = null;

                int categoryCount;
                if (history.CategoryCounts.TryGetValue(evt.Category, out categoryCount) && categoryCount > 0)
                {
                    score += categoryCount * CategoryWeight;
                    reason = string.Format("You often search {0} events", evt.Category);
                }

                foreach (var pair in history.KeywordCounts)
                {
                    if (TextContains(evt, pair.Key))
                    {
                        score += pair.Value * KeywordWeight;
                        if (reason == null)
                            reason = string.Format("Related to your search for \"{0}\"", pair.Key);
                    }
                }

                if (score == 0)
                    continue; // nothing in the user's history relates to this event

                score += 6 - evt.Priority; // small boost for important events
                queue.Enqueue(new EventRecommendation(evt, score, reason));
            }

            var results = new List<EventRecommendation>();
            while (results.Count < count && queue.Count > 0)
                results.Add(queue.Dequeue());

            // Fallback / top-up: fill remaining slots with featured events
            if (results.Count < count)
            {
                var alreadyChosen = new HashSet<Guid>(results.Select(r => r.Event.Id));

                foreach (var evt in repo.GetFeaturedEvents(count + alreadyChosen.Count))
                {
                    if (results.Count >= count) break;
                    if (alreadyChosen.Contains(evt.Id)) continue;
                    if (exclude != null && exclude.Contains(evt.Id)) continue;

                    results.Add(new EventRecommendation(evt, 0, "Featured event"));
                    alreadyChosen.Add(evt.Id);
                }
            }

            return results;
        }

        private static bool TextContains(LocalEvent evt, string keyword)
        {
            return Contains(evt.Title, keyword)
                || Contains(evt.Description, keyword)
                || Contains(evt.Location, keyword);
        }

        private static bool Contains(string text, string keyword)
        {
            return text != null &&
                   text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}