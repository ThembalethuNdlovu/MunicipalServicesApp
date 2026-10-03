using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Singleton that records user searches.
    /// - Stack: recent searches, newest first (LIFO)
    /// - Dictionaries: how often each category / keyword has been searched
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

        private SearchHistoryService() { }

        public int TotalSearches
        {
            get { return _history.Count; }
        }

        /// <summary>
        /// Records a search. Searches with no keyword and no category are ignored
        /// because they tell us nothing about the user's interests.
        /// </summary>
        public void RecordSearch(string keyword, string category, DateTime? from, DateTime? to)
        {
            var record = new SearchRecord(keyword, category, from, to);
            if (record.Keyword == null && record.Category == null)
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
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            int current;
            counts.TryGetValue(key, out current);
            counts[key] = current + 1;
        }
    }
}