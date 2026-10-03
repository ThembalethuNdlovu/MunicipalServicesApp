using System;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// Represents a local event or municipal announcement.
    /// Priority: 1 = highest (urgent), 5 = lowest.
    /// </summary>
    public class LocalEvent
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Category { get; private set; }
        public string Location { get; private set; }
        public DateTime Date { get; private set; }
        public int Priority { get; private set; }
        public bool IsAnnouncement { get; private set; }

        public LocalEvent(string title, string description, string category,
                          string location, DateTime date,
                          int priority = 3, bool isAnnouncement = false)
        {
            Id = Guid.NewGuid();
            Title = title;
            Description = description;
            Category = category;
            Location = location;
            Date = date;
            Priority = priority;
            IsAnnouncement = isAnnouncement;
        }

        public override string ToString()
        {
            return string.Format("{0:dd MMM yyyy} - {1} ({2})", Date, Title, Category);
        }
    }
}