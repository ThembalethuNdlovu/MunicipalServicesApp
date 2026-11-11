using System;
using System.Collections.Generic;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Central store for all reported issues. Acts as the single source of truth.
    /// Other services (the Part 3 indexes) subscribe to the events below so they
    /// stay in sync without this class knowing about them.
    /// </summary>
    public class IssueRepository
    {
        // Simple singleton pattern: one shared instance for the whole app session.
        private static readonly IssueRepository _instance = new IssueRepository();
        public static IssueRepository Instance => _instance;

        private readonly List<Issue> _issues;

        /// <summary>Raised after an issue has been added.</summary>
        public event Action<Issue> IssueAdded;

        /// <summary>Raised after an issue's status has changed.</summary>
        public event Action<Issue> IssueUpdated;

        private IssueRepository()
        {
            _issues = new List<Issue>();
        }

        public void AddIssue(Issue issue)
        {
            if (issue == null)
                throw new ArgumentNullException(nameof(issue));

            _issues.Add(issue);
            IssueAdded?.Invoke(issue);
        }

        /// <summary>Read-only view: callers cannot change the stored list.</summary>
        public IReadOnlyList<Issue> GetAllIssues()
        {
            return _issues.AsReadOnly();
        }

        public int Count => _issues.Count;

        /// <summary>
        /// Moves an issue to its next status and notifies subscribers.
        /// Returns false if the issue was already resolved.
        /// </summary>
        public bool AdvanceStatus(Issue issue)
        {
            if (issue == null)
                throw new ArgumentNullException(nameof(issue));

            bool changed = issue.AdvanceStatus();
            if (changed)
                IssueUpdated?.Invoke(issue);
            return changed;
        }

        /// <summary>
        /// Baseline lookup: scans every issue, O(n). Kept so the report can compare
        /// it against the O(log n) tree indexes built in Part 3.
        /// </summary>
        public Issue FindByTrackingNumberLinear(string trackingNumber)
        {
            int sequence;
            if (!Issue.TryParseTrackingNumber(trackingNumber, out sequence))
                return null;

            foreach (Issue issue in _issues)
            {
                if (issue.Sequence == sequence)
                    return issue;
            }
            return null;
        }
    }
}