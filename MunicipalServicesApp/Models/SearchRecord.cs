using System;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// A single search the user performed on the Local Events page.
    /// </summary>
    public class SearchRecord
    {
        public string Keyword { get; private set; }
        public string Category { get; private set; }
        public DateTime? From { get; private set; }
        public DateTime? To { get; private set; }
        public DateTime Timestamp { get; private set; }

        public SearchRecord(string keyword, string category, DateTime? from, DateTime? to)
        {
            Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim().ToLowerInvariant();
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
            From = from;
            To = to;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            string kw = Keyword ?? "any keyword";
            string cat = Category ?? "all categories";
            return string.Format("{0:HH:mm} - \"{1}\" in {2}", Timestamp, kw, cat);
        }
    }
}