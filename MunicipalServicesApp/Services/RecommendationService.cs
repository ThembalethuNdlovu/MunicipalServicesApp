using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Suggests events based on the user's search history.
    ///
    /// Scoring (all search contributions are multiplied by a recency weight):
    ///   category match   : weighted searches x CategoryWeight
    ///   keyword match    : weighted searches x KeywordWeight
    ///   date preference  : DateWeight x recency x proximity to a searched date range
    ///   priority bonus   : (6 - event priority)
    ///
    /// Recency weight = 0.5 ^ (age in hours / HalfLifeHours), so a search loses half
    /// its influence every HalfLifeHours.
    /// A max-first MinPriorityQueue picks the best-scoring events.
    /// </summary>
    public static class RecommendationService
    {
        // ---------- Tunable scoring constants ----------
        private const double CategoryWeight = 3.0;
        private const double KeywordWeight = 2.0;
        private const double DateWeight = 4.0;

        /// <summary>A search loses half of its influence after this many hours.</summary>
        private const double HalfLifeHours = 24.0;

        /// <summary>Events further than this from a searched date range get no date bonus.</summary>
        private const int DateRangeDays = 14;

        /// <summary>Scores are fractional; multiply and round to fit EventRecommendation's int score.</summary>
        private const int ScoreScale = 10;

        /// <param name="count">Maximum number of recommendations.</param>
        /// <param name="exclude">Optional set of event IDs to leave out (e.g. already on screen).</param>
        public static List<EventRecommendation> GetRecommendations(int count, HashSet<Guid> exclude = null)
        {
            var repo = EventRepository.Instance;
            var history = SearchHistoryService.Instance;
            DateTime now = DateTime.Now; // one clock reading per call keeps ordering stable

            // Build the user's interest profile from the timestamped searches
            var categoryWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var keywordWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var dateWindows = new List<DateWindow>();
            BuildInterestProfile(history.GetTimeline(), now, categoryWeights, keywordWeights, dateWindows);

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
                double score = 0;
                string reason = null;

                double categoryWeight;
                if (evt.Category != null && categoryWeights.TryGetValue(evt.Category, out categoryWeight))
                {
                    score += categoryWeight * CategoryWeight;
                    reason = string.Format("Matches your recent interest in {0} events", evt.Category);
                }

                foreach (var pair in keywordWeights)
                {
                    if (TextContains(evt, pair.Key))
                    {
                        score += pair.Value * KeywordWeight;
                        if (reason == null)
                            reason = string.Format("Related to your search for \"{0}\"", pair.Key);
                    }
                }

                int gapDays;
                double dateBonus = DateBonus(evt.Date, dateWindows, out gapDays);
                if (dateBonus > 0)
                {
                    score += dateBonus;
                    if (reason == null)
                    {
                        reason = gapDays == 0
                            ? "Happens within the dates you searched"
                            : "Happens close to the dates you searched";
                    }
                }

                if (score <= 0)
                    continue; // nothing in the user's history relates to this event

                score += 6 - evt.Priority; // small boost for important events

                int scaledScore = (int)Math.Round(score * ScoreScale);
                queue.Enqueue(new EventRecommendation(evt, scaledScore, reason));
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

        // =====================================================================
        //  Interest profile
        // =====================================================================
        private static void BuildInterestProfile(
            List<SearchEntry> timeline,
            DateTime now,
            Dictionary<string, double> categoryWeights,
            Dictionary<string, double> keywordWeights,
            List<DateWindow> dateWindows)
        {
            foreach (var entry in timeline)
            {
                double recency = RecencyWeight(entry.Timestamp, now);

                if (entry.Category != null)
                    AddWeight(categoryWeights, entry.Category, recency);

                if (entry.Keyword != null)
                    AddWeight(keywordWeights, entry.Keyword, recency);

                if (entry.From.HasValue || entry.To.HasValue)
                    dateWindows.Add(new DateWindow(entry.From, entry.To, recency));
            }
        }

        /// <summary>1.0 for a search made just now, 0.5 after one half-life, and so on.</summary>
        private static double RecencyWeight(DateTime timestamp, DateTime now)
        {
            double ageHours = (now - timestamp).TotalHours;
            if (ageHours < 0) ageHours = 0;
            return Math.Pow(0.5, ageHours / HalfLifeHours);
        }

        private static void AddWeight(Dictionary<string, double> weights, string key, double weight)
        {
            double current;
            weights.TryGetValue(key, out current);
            weights[key] = current + weight;
        }

        // =====================================================================
        //  Date preference
        // =====================================================================
        /// <summary>
        /// Best date bonus across all searched date ranges (not summed, so many date
        /// searches cannot overwhelm the other signals). gapDays is how far the event
        /// is from the range that produced the bonus (0 = inside it).
        /// </summary>
        private static double DateBonus(DateTime eventDate, List<DateWindow> windows, out int gapDays)
        {
            gapDays = -1;
            double best = 0;
            DateTime day = eventDate.Date;

            foreach (var window in windows)
            {
                int gap;
                if (day < window.Start) gap = (window.Start - day).Days;
                else if (day > window.End) gap = (day - window.End).Days;
                else gap = 0;

                if (gap > DateRangeDays)
                    continue;

                // 1.0 inside the range, fading linearly to 0 just past DateRangeDays
                double proximity = 1.0 - (double)gap / (DateRangeDays + 1);
                double bonus = DateWeight * window.Weight * proximity;

                if (bonus > best)
                {
                    best = bonus;
                    gapDays = gap;
                }
            }

            return best;
        }

        private class DateWindow
        {
            public DateTime Start { get; private set; }
            public DateTime End { get; private set; }
            public double Weight { get; private set; }

            public DateWindow(DateTime? from, DateTime? to, double weight)
            {
                // Only one end given: treat it as a single day
                DateTime a = (from ?? to).Value.Date;
                DateTime b = (to ?? from).Value.Date;
                Start = a <= b ? a : b;
                End = a <= b ? b : a;
                Weight = weight;
            }
        }

        // =====================================================================
        //  Text matching
        // =====================================================================
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