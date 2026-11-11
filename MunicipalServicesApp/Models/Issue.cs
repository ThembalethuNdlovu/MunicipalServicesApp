using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace MunicipalServicesApp.Models
{
    /// <summary>Lifecycle of a service request, in order.</summary>
    public enum RequestStatus
    {
        Submitted = 0,
        UnderReview = 1,
        InProgress = 2,
        Resolved = 3
    }

    /// <summary>One entry in a request's status history.</summary>
    public class StatusChange
    {
        public RequestStatus Status { get; private set; }
        public DateTime Timestamp { get; private set; }

        public StatusChange(RequestStatus status, DateTime timestamp)
        {
            Status = status;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Represents a single citizen-reported municipal issue / service request.
    /// Priority: 1 = highest (urgent), 5 = lowest (same scale as LocalEvent).
    /// </summary>
    public class Issue
    {
        // Sequence numbers are handed out in order: 1, 2, 3, ...
        private static int _lastSequence = 0;

        private static readonly Dictionary<string, int> CategoryPriorities =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Public Safety", 1 },
                { "Utilities (Water/Electricity)", 2 },
                { "Sanitation", 2 },
                { "Roads", 3 },
                { "Waste Management", 3 },
                { "Parks and Recreation", 4 },
                { "Other", 5 }
            };

        private readonly List<StatusChange> _statusHistory = new List<StatusChange>();
        private int _priority;

        public Guid Id { get; private set; }

        /// <summary>Numeric key used by the tree indexes (1, 2, 3, ...).</summary>
        public int Sequence { get; private set; }

        /// <summary>Citizen-friendly identifier, e.g. SR-000123.</summary>
        public string TrackingNumber
        {
            get { return "SR-" + Sequence.ToString("D6", CultureInfo.InvariantCulture); }
        }

        public string Location { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public List<string> AttachmentPaths { get; private set; }
        public DateTime DateReported { get; private set; }

        public RequestStatus Status { get; private set; }
        public DateTime LastUpdated { get; private set; }

        /// <summary>Service area this request belongs to (assigned later; used by the graph).</summary>
        public string Area { get; set; }

        /// <summary>1 (urgent) to 5 (low). Values outside the range are clamped.</summary>
        public int Priority
        {
            get { return _priority; }
            set { _priority = Math.Max(1, Math.Min(5, value)); }
        }

        public IReadOnlyList<StatusChange> StatusHistory
        {
            get { return _statusHistory; }
        }

        public string StatusText
        {
            get { return GetStatusText(Status); }
        }

        public Issue(string location, string category, string description)
            : this(location, category, description, DateTime.Now)
        {
        }

        /// <summary>Also used by the demo-data seeder to create requests reported in the past.</summary>
        public Issue(string location, string category, string description, DateTime dateReported)
        {
            Id = Guid.NewGuid();
            Sequence = Interlocked.Increment(ref _lastSequence);
            Location = location;
            Category = category;
            Description = description;
            AttachmentPaths = new List<string>();
            DateReported = dateReported;

            Status = RequestStatus.Submitted;
            LastUpdated = dateReported;
            Area = "Unassigned";
            Priority = DefaultPriorityFor(category);

            _statusHistory.Add(new StatusChange(Status, dateReported));
        }

        public void AddAttachment(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                AttachmentPaths.Add(filePath);
            }
        }

        /// <summary>Moves the request to its next status now. Returns false if already resolved.</summary>
        public bool AdvanceStatus()
        {
            return AdvanceStatus(DateTime.Now);
        }

        /// <summary>Moves the request to its next status at the given time.</summary>
        public bool AdvanceStatus(DateTime when)
        {
            if (Status == RequestStatus.Resolved)
                return false;

            Status = (RequestStatus)((int)Status + 1);
            LastUpdated = when;
            _statusHistory.Add(new StatusChange(Status, when));
            return true;
        }

        public static int DefaultPriorityFor(string category)
        {
            int priority;
            if (category != null && CategoryPriorities.TryGetValue(category, out priority))
                return priority;
            return 5;
        }

        public static string GetStatusText(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Submitted: return "Submitted";
                case RequestStatus.UnderReview: return "Under Review";
                case RequestStatus.InProgress: return "In Progress";
                default: return "Resolved";
            }
        }

        /// <summary>
        /// Accepts "SR-000123", "sr-123" or just "123" and returns the numeric sequence.
        /// </summary>
        public static bool TryParseTrackingNumber(string input, out int sequence)
        {
            sequence = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string text = input.Trim();
            if (text.StartsWith("SR-", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(3);

            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out sequence)
                   && sequence > 0;
        }
    }
}