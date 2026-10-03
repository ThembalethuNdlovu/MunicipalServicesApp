using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Singleton repository for local events and announcements.
    /// Each data structure is chosen for a specific job (see comments).
    /// </summary>
    public class EventRepository
    {
        // ---------- Singleton ----------
        private static EventRepository _instance;
        private static readonly object _lock = new object();

        public static EventRepository Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new EventRepository();
                    return _instance;
                }
            }
        }

        // ---------- Data structures ----------

        // Hash table: O(1) lookup of an event by its ID
        private readonly Dictionary<Guid, LocalEvent> _eventsById =
            new Dictionary<Guid, LocalEvent>();

        // Sorted dictionary: events grouped by day, always in date order
        private readonly SortedDictionary<DateTime, List<LocalEvent>> _eventsByDate =
            new SortedDictionary<DateTime, List<LocalEvent>>();

        // Dictionary: events grouped by category (case-insensitive)
        private readonly Dictionary<string, List<LocalEvent>> _eventsByCategory =
            new Dictionary<string, List<LocalEvent>>(StringComparer.OrdinalIgnoreCase);

        // Sets: unique categories and unique event dates
        private readonly HashSet<string> _categories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly SortedSet<DateTime> _eventDates = new SortedSet<DateTime>();

        // Queue: announcements shown one after another (FIFO rotation)
        private readonly Queue<LocalEvent> _announcementQueue = new Queue<LocalEvent>();

        private EventRepository()
        {
            SeedSampleData();
        }

        // ---------- Adding ----------
        public void AddEvent(LocalEvent evt)
        {
            if (evt == null) throw new ArgumentNullException("evt");

            _eventsById[evt.Id] = evt;

            DateTime dayKey = evt.Date.Date;
            List<LocalEvent> sameDay;
            if (!_eventsByDate.TryGetValue(dayKey, out sameDay))
            {
                sameDay = new List<LocalEvent>();
                _eventsByDate[dayKey] = sameDay;
            }
            sameDay.Add(evt);

            List<LocalEvent> sameCategory;
            if (!_eventsByCategory.TryGetValue(evt.Category, out sameCategory))
            {
                sameCategory = new List<LocalEvent>();
                _eventsByCategory[evt.Category] = sameCategory;
            }
            sameCategory.Add(evt);

            _categories.Add(evt.Category);
            _eventDates.Add(dayKey);

            if (evt.IsAnnouncement)
                _announcementQueue.Enqueue(evt);
        }

        // ---------- Retrieval ----------
        public LocalEvent GetById(Guid id)
        {
            LocalEvent evt;
            return _eventsById.TryGetValue(id, out evt) ? evt : null;
        }

        public List<LocalEvent> GetAllEvents()
        {
            var result = new List<LocalEvent>();
            foreach (var pair in _eventsByDate)
                result.AddRange(pair.Value.OrderBy(e => e.Date));
            return result;
        }

        public List<LocalEvent> GetByCategory(string category)
        {
            List<LocalEvent> list;
            if (category != null && _eventsByCategory.TryGetValue(category, out list))
                return list.OrderBy(e => e.Date).ToList();
            return new List<LocalEvent>();
        }

        public List<string> GetCategories()
        {
            return _categories.OrderBy(c => c).ToList();
        }

        public List<DateTime> GetEventDates()
        {
            return _eventDates.ToList();
        }

        /// <summary>
        /// Search by optional keyword, category and date range.
        /// Because dates are in a SortedDictionary, we can stop as soon as
        /// we pass the end of the requested range.
        /// </summary>
        public List<LocalEvent> Search(string keyword, string category,
                                       DateTime? from, DateTime? to)
        {
            var results = new List<LocalEvent>();
            bool hasCategory = !string.IsNullOrWhiteSpace(category);
            bool hasKeyword = !string.IsNullOrWhiteSpace(keyword);

            foreach (var pair in _eventsByDate)
            {
                if (from.HasValue && pair.Key < from.Value.Date) continue;
                if (to.HasValue && pair.Key > to.Value.Date) break;

                foreach (var evt in pair.Value.OrderBy(e => e.Date))
                {
                    if (hasCategory &&
                        !string.Equals(evt.Category, category, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (hasKeyword && !MatchesKeyword(evt, keyword))
                        continue;

                    results.Add(evt);
                }
            }
            return results;
        }

        private static bool MatchesKeyword(LocalEvent evt, string keyword)
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

        // ---------- Priority queue: featured events ----------
        /// <summary>
        /// Returns the most important upcoming events (lowest priority number first,
        /// ties broken by earliest date) using a min-heap priority queue.
        /// </summary>
        public List<LocalEvent> GetFeaturedEvents(int count)
        {
            var queue = new MinPriorityQueue<LocalEvent>((a, b) =>
            {
                int byPriority = a.Priority.CompareTo(b.Priority);
                return byPriority != 0 ? byPriority : a.Date.CompareTo(b.Date);
            });

            foreach (var evt in _eventsById.Values)
            {
                if (evt.Date.Date >= DateTime.Today)
                    queue.Enqueue(evt);
            }

            var result = new List<LocalEvent>();
            while (result.Count < count && queue.Count > 0)
                result.Add(queue.Dequeue());
            return result;
        }

        // ---------- Queue: announcement rotation ----------
        /// <summary>
        /// Takes the next announcement from the front of the queue and puts it
        /// back at the end, so announcements cycle indefinitely.
        /// </summary>
        public LocalEvent GetNextAnnouncement()
        {
            if (_announcementQueue.Count == 0) return null;

            LocalEvent next = _announcementQueue.Dequeue();
            _announcementQueue.Enqueue(next);
            return next;
        }

        // ---------- Sample data ----------
        private void SeedSampleData()
        {
            DateTime today = DateTime.Today;

            // Community
            AddEvent(new LocalEvent("Community Clean-Up Day",
                "Join your neighbours to clean up the park and river banks. Gloves and bags provided.",
                "Community", "Central Park", today.AddDays(3).AddHours(8), 2));
            AddEvent(new LocalEvent("Ward Committee Meeting",
                "Monthly public meeting to discuss service delivery in your ward.",
                "Community", "Municipal Hall", today.AddDays(7).AddHours(18), 2));
            AddEvent(new LocalEvent("Neighbourhood Watch Info Session",
                "Learn how to start or join a neighbourhood watch in your area.",
                "Community", "Community Library", today.AddDays(12).AddHours(17), 4));

            // Sports
            AddEvent(new LocalEvent("Soccer Tournament",
                "Inter-ward youth soccer tournament. Everyone is welcome to cheer on their ward.",
                "Sports", "Municipal Stadium", today.AddDays(5).AddHours(9), 3));
            AddEvent(new LocalEvent("Fun Run 5km",
                "Family-friendly 5km fun run through the town centre.",
                "Sports", "Town Centre", today.AddDays(14).AddHours(7), 3));

            // Culture
            AddEvent(new LocalEvent("Heritage Market",
                "Local crafters, food stalls and live traditional music.",
                "Culture", "Market Square", today.AddDays(9).AddHours(10), 3));
            AddEvent(new LocalEvent("Youth Poetry Evening",
                "An open-mic evening celebrating young voices in our community.",
                "Culture", "Community Library", today.AddDays(16).AddHours(18), 4));

            // Health
            AddEvent(new LocalEvent("Free Health Screening",
                "Free blood pressure, glucose and general health checks.",
                "Health", "Local Clinic", today.AddDays(4).AddHours(9), 2));
            AddEvent(new LocalEvent("Vaccination Drive",
                "Free childhood vaccinations. Please bring the child's health card.",
                "Health", "Local Clinic", today.AddDays(10).AddHours(8), 1));

            // Education
            AddEvent(new LocalEvent("Career Expo",
                "Meet employers and training providers. Bring copies of your CV.",
                "Education", "Municipal Hall", today.AddDays(18).AddHours(9), 3));

            // Announcements (also feed the announcement queue)
            AddEvent(new LocalEvent("Planned Water Interruption",
                "Water supply will be interrupted for maintenance. Please store water in advance.",
                "Utilities", "Wards 3 and 4", today.AddDays(2).AddHours(6), 1, true));
            AddEvent(new LocalEvent("Electricity Maintenance Notice",
                "Scheduled power maintenance. Expect outages during the stated hours.",
                "Utilities", "Industrial Area", today.AddDays(6).AddHours(9), 1, true));
            AddEvent(new LocalEvent("Road Closure: Main Street",
                "Main Street closed for resurfacing. Please use alternative routes.",
                "Roads", "Main Street", today.AddDays(1).AddHours(7), 2, true));
            AddEvent(new LocalEvent("Refuse Collection Schedule Change",
                "Refuse collection moves by one day due to the upcoming public holiday.",
                "Sanitation", "All Wards", today.AddDays(8).AddHours(6), 3, true));
        }
    }
}