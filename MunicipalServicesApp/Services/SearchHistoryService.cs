using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// One timestamped search. Used by the recommendation engine to weight
    /// recent searches more heavily and to learn the user's preferred dates.
    /// </summary>
    public class SearchEntry
    {
        public DateTime Timestamp { get; private set; }
        public string Keyword { get; private set; }
        public string Category { get; private set; }
        public DateTime? From { get; private set; }
        public DateTime? To { get; private set; }

        public SearchEntry(DateTime timestamp, string keyword, string category,
                           DateTime? from, DateTime? to)
        {
            Timestamp = timestamp;
            Keyword = keyword;
            Category = category;
            From = from.HasValue ? (DateTime?)from.Value.Date : null;
            To = to.HasValue ? (DateTime?)to.Value.Date : null;
        }
    }

    /// <summary>
    /// Singleton that records user searches.
    /// - Stack: recent searches, newest first (LIFO)
    /// - Dictionaries: how often each category / keyword has been searched
    /// - Timeline: every search with its timestamp and date range (recency + date preferences)
    /// </summary>
    public class SearchHistoryService
    {
        private static SearchHistoryService _instance;
        private static readonly object _lock = new object();

        public static SearchHistoryService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new SearchHistoryService();
                    return _instance;
                }
            }
        }

        private readonly Stack<SearchRecord> _history = new Stack<SearchRecord>();

        private readonly Dictionary<string, int> _categoryCounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, int> _keywordCounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SearchEntry> _timeline = new List<SearchEntry>();

        private SearchHistoryService() { }

        public int TotalSearches
        {
            get { return _history.Count; }
        }

        /// <summary>
        /// Records a search stamped with the current time.
        /// </summary>
        public void RecordSearch(string keyword, string category, DateTime? from, DateTime? to)
        {
            RecordSearch(keyword, category, from, to, DateTime.Now);
        }

        /// <summary>
        /// Records a search with an explicit timestamp (useful for tests and demo data).
        /// Searches with no keyword, category or dates are ignored. Searches with dates
        /// only feed the date preferences; they are kept out of the recent-search list
        /// and the keyword/category counts.
        /// </summary>
        public void RecordSearch(string keyword, string category, DateTime? from, DateTime? to,
                                 DateTime timestamp)
        {
            var record = new SearchRecord(keyword, category, from, to);

            bool hasContent = record.Keyword != null || record.Category != null;
            bool hasDates = from.HasValue || to.HasValue;

            if (!hasContent && !hasDates)
                return;

            _timeline.Add(new SearchEntry(timestamp, record.Keyword, record.Category, from, to));

            if (!hasContent)
                return;

            _history.Push(record);

            if (record.Category != null)
                Increment(_categoryCounts, record.Category);

            if (record.Keyword != null)
                Increment(_keywordCounts, record.Keyword);
        }

        /// <summary>Most recent searches first.</summary>
        public List<SearchRecord> GetRecentSearches(int count)
        {
            return _history.Take(count).ToList();
        }

        /// <summary>A copy of every recorded search, oldest first.</summary>
        public List<SearchEntry> GetTimeline()
        {
            return new List<SearchEntry>(_timeline);
        }

        public IReadOnlyDictionary<string, int> CategoryCounts
        {
            get { return _categoryCounts; }
        }

        public IReadOnlyDictionary<string, int> KeywordCounts
        {
            get { return _keywordCounts; }
        }

        public List<string> GetTopCategories(int count)
        {
            return _categoryCounts
                .OrderByDescending(p => p.Value)
                .ThenBy(p => p.Key)
                .Take(count)
                .Select(p => p.Key)
                .ToList();
        }

        public void Clear()
        {
            _history.Clear();
            _categoryCounts.Clear();
            _keywordCounts.Clear();
            _timeline.Clear();
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            int current;
            counts.TryGetValue(key, out current);
            counts[key] = current + 1;
        }
    }
}