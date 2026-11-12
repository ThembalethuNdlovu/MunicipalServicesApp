using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.DataStructures;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Key for the date index. The red-black tree rejects duplicate keys, but two requests
    /// can be reported at exactly the same moment, so the key is the date plus the
    /// request's Sequence number. That makes every key unique and keeps same-date
    /// requests in the order they were reported.
    /// </summary>
    public struct DateSequenceKey : IComparable<DateSequenceKey>
    {
        public DateTime Date { get; private set; }
        public int Sequence { get; private set; }

        public DateSequenceKey(DateTime date, int sequence)
        {
            Date = date;
            Sequence = sequence;
        }

        public int CompareTo(DateSequenceKey other)
        {
            int byDate = Date.CompareTo(other.Date);
            return byDate != 0 ? byDate : Sequence.CompareTo(other.Sequence);
        }
    }

    /// <summary>
    /// One node value in the category tree: either a category (Issue is null)
    /// or a single request. ToString gives the text a TreeView can show, and it is
    /// built when asked for, so a request's current status is always up to date.
    /// </summary>
    public class CategoryTreeEntry
    {
        public string Label { get; private set; }
        public Issue Issue { get; private set; }

        public bool IsRequest
        {
            get { return Issue != null; }
        }

        public CategoryTreeEntry(string label, Issue issue)
        {
            Label = label;
            Issue = issue;
        }

        public override string ToString()
        {
            if (Issue == null)
                return Label;

            return Issue.TrackingNumber + " - " + Issue.StatusText + " - " + Issue.Location;
        }
    }

    /// <summary>Number of requests in one service area (all of them, and the ones not yet resolved).</summary>
    public class AreaSummary
    {
        public string Area { get; private set; }
        public int Total { get; private set; }
        public int Open { get; private set; }

        public AreaSummary(string area, int total, int open)
        {
            Area = area;
            Total = total;
            Open = open;
        }
    }

    /// <summary>
    /// Facade over the Part 3 data structures. It keeps four indexes in step with the
    /// IssueRepository, so the form never has to scan the whole list:
    ///   - AVL tree, key = Sequence             : tracking-number lookup, O(log n)
    ///   - Red-black tree, key = date + sequence: date-range queries, O(log n + k)
    ///   - Binary heap (unresolved requests)    : "next to handle", Peek is O(1)
    ///   - General tree                         : Municipality -> Category -> Request
    /// Area counts are worked out in one pass over the requests when asked for.
    ///
    /// Start-up order: everything already in the repository is indexed first, then the
    /// service subscribes to IssueAdded / IssueUpdated, so no request is missed.
    ///
    /// Pass null as the repository to get a service with no events, fed by calling
    /// Index and Refresh yourself. The self-tests do this, because the real repository
    /// is a singleton that must not be filled with test data.
    /// </summary>
    public class ServiceRequestService : IDisposable
    {
        private static ServiceRequestService _instance;

        /// <summary>The shared service attached to the shared repository (created on first use).</summary>
        public static ServiceRequestService Instance
        {
            get { return _instance ?? (_instance = new ServiceRequestService(IssueRepository.Instance)); }
        }

        private readonly IssueRepository _repository;
        private readonly List<Issue> _all = new List<Issue>();

        private readonly AvlTree<int, Issue> _sequenceIndex = new AvlTree<int, Issue>();

        private readonly RedBlackTree<DateSequenceKey, Issue> _dateIndex =
            new RedBlackTree<DateSequenceKey, Issue>(Comparer<DateSequenceKey>.Default);

        private readonly BinaryHeap<Issue> _urgencyHeap = new BinaryHeap<Issue>(IssueUrgencyComparer.Instance);

        private readonly GeneralTree<CategoryTreeEntry> _categoryTree =
            new GeneralTree<CategoryTreeEntry>(new CategoryTreeEntry("Municipality", null));

        private readonly Dictionary<string, GeneralTreeNode<CategoryTreeEntry>> _categoryNodes =
            new Dictionary<string, GeneralTreeNode<CategoryTreeEntry>>(StringComparer.OrdinalIgnoreCase);

        public ServiceRequestService(IssueRepository repository)
        {
            _repository = repository;

            if (_repository != null)
            {
                // 1. Index what is already stored (the demo data may have been seeded before this ran)
                foreach (Issue issue in _repository.GetAllIssues())
                    Index(issue);

                // 2. Then follow every change
                _repository.IssueAdded += OnIssueAdded;
                _repository.IssueUpdated += OnIssueUpdated;
            }
        }

        public void Dispose()
        {
            if (_repository != null)
            {
                _repository.IssueAdded -= OnIssueAdded;
                _repository.IssueUpdated -= OnIssueUpdated;
            }
        }

        // =====================================================================
        //  Read-only information
        // =====================================================================
        /// <summary>Number of requests indexed.</summary>
        public int Count
        {
            get { return _all.Count; }
        }

        /// <summary>Requests that are not yet resolved (the size of the heap).</summary>
        public int OpenCount
        {
            get { return _urgencyHeap.Count; }
        }

        /// <summary>Comparisons used by the most recent FindByTrackingNumber (0 if the text was not a valid number).</summary>
        public int LastLookupComparisons { get; private set; }

        /// <summary>How many times the heap has been rebuilt with BuildHeap after a status change.</summary>
        public int HeapRebuildCount { get; private set; }

        // The structures below are exposed so the structure demo panel can show their
        // diagrams and counters. Look, do not change: all changes go through this class.
        public AvlTree<int, Issue> SequenceIndex
        {
            get { return _sequenceIndex; }
        }

        public RedBlackTree<DateSequenceKey, Issue> DateIndex
        {
            get { return _dateIndex; }
        }

        public BinaryHeap<Issue> UrgencyHeap
        {
            get { return _urgencyHeap; }
        }

        public GeneralTree<CategoryTreeEntry> CategoryTree
        {
            get { return _categoryTree; }
        }

        // =====================================================================
        //  Keeping the indexes in step
        // =====================================================================
        private void OnIssueAdded(Issue issue)
        {
            Index(issue);
        }

        private void OnIssueUpdated(Issue issue)
        {
            Refresh(issue);
        }

        /// <summary>
        /// Adds a request to every index. Returns false (and changes nothing) if it was
        /// already indexed, so a request can never be counted twice.
        /// </summary>
        public bool Index(Issue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            // The AVL tree is the master list: a duplicate Sequence is rejected here
            if (!_sequenceIndex.Insert(issue.Sequence, issue))
                return false;

            // Work out the service area, unless one was already assigned
            if (string.IsNullOrWhiteSpace(issue.Area) ||
                string.Equals(issue.Area, ServiceAreaCatalog.Unassigned, StringComparison.OrdinalIgnoreCase))
            {
                issue.Area = ServiceAreaCatalog.FindArea(issue.Location);
            }

            _dateIndex.Insert(new DateSequenceKey(issue.DateReported, issue.Sequence), issue);
            _all.Add(issue);
            AddToCategoryTree(issue);

            if (issue.Status != RequestStatus.Resolved)
                _urgencyHeap.Enqueue(issue);

            return true;
        }

        /// <summary>
        /// Call after a request's status (or priority) has changed. The heap order depends
        /// on values that just changed, so the heap is rebuilt in O(n) with BuildHeap,
        /// which also drops a request that has just been resolved. A request that was never
        /// indexed is indexed instead.
        /// </summary>
        public void Refresh(Issue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            Issue existing;
            if (!_sequenceIndex.TryGetValue(issue.Sequence, out existing))
            {
                Index(issue);
                return;
            }

            _urgencyHeap.BuildHeap(_all.Where(i => i.Status != RequestStatus.Resolved));
            HeapRebuildCount++;
        }

        private void AddToCategoryTree(Issue issue)
        {
            string category = string.IsNullOrWhiteSpace(issue.Category) ? "Other" : issue.Category.Trim();

            GeneralTreeNode<CategoryTreeEntry> categoryNode;
            if (!_categoryNodes.TryGetValue(category, out categoryNode))
            {
                categoryNode = _categoryTree.AddChild(_categoryTree.Root, new CategoryTreeEntry(category, null));
                _categoryNodes[category] = categoryNode;
            }

            _categoryTree.AddChild(categoryNode, new CategoryTreeEntry(issue.TrackingNumber, issue));
        }

        // =====================================================================
        //  Queries
        // =====================================================================
        /// <summary>
        /// Finds a request from "SR-000123", "sr-123" or "123". Returns null if the text
        /// is not a valid number or no such request exists. Uses the AVL tree: O(log n).
        /// </summary>
        public Issue FindByTrackingNumber(string trackingNumber)
        {
            int sequence;
            if (!Issue.TryParseTrackingNumber(trackingNumber, out sequence))
            {
                LastLookupComparisons = 0;
                return null;
            }

            Issue found;
            bool ok = _sequenceIndex.TryGetValue(sequence, out found);
            LastLookupComparisons = _sequenceIndex.LastOperationComparisons;
            return ok ? found : null;
        }

        /// <summary>All requests in tracking-number order (an in-order walk of the AVL tree).</summary>
        public List<Issue> GetAllRequests()
        {
            var list = new List<Issue>(_all.Count);
            foreach (KeyValuePair<int, Issue> pair in _sequenceIndex.InOrder())
                list.Add(pair.Value);
            return list;
        }

        /// <summary>
        /// Requests reported from the start of fromDate to the end of toDate (both whole
        /// days, inclusive), oldest first. Uses the red-black tree range query.
        /// Returns nothing if fromDate is after toDate.
        /// </summary>
        public List<Issue> GetByDateRange(DateTime fromDate, DateTime toDate)
        {
            DateTime start = fromDate.Date;
            DateTime end = toDate.Date.AddDays(1).AddTicks(-1);

            var low = new DateSequenceKey(start, int.MinValue);
            var high = new DateSequenceKey(end, int.MaxValue);

            var list = new List<Issue>();
            foreach (KeyValuePair<DateSequenceKey, Issue> pair in _dateIndex.GetRange(low, high))
                list.Add(pair.Value);
            return list;
        }

        /// <summary>The most urgent unresolved request, or null if there is none. O(1).</summary>
        public Issue NextToHandle
        {
            get
            {
                Issue next;
                return _urgencyHeap.TryPeek(out next) ? next : null;
            }
        }

        /// <summary>
        /// The most urgent unresolved requests, most urgent first (at most 'max').
        /// Works on a copy, so the heap itself is not changed.
        /// </summary>
        public List<Issue> GetUrgentQueue(int max)
        {
            if (max <= 0)
                return new List<Issue>();

            return _urgencyHeap.GetSortedCopy().Take(max).ToList();
        }

        /// <summary>The requests under one category (not case-sensitive), in the order they were indexed.</summary>
        public List<Issue> GetRequestsInCategory(string category)
        {
            var list = new List<Issue>();

            string key = string.IsNullOrWhiteSpace(category) ? "Other" : category.Trim();
            GeneralTreeNode<CategoryTreeEntry> node;
            if (_categoryNodes.TryGetValue(key, out node))
            {
                foreach (GeneralTreeNode<CategoryTreeEntry> child in node.Children)
                    list.Add(child.Value.Issue);
            }

            list.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
            return list;
        }

        /// <summary>
        /// Requests per service area: every catalogue area (even with none), then
        /// "Unassigned", then any other area name found on a request.
        /// </summary>
        public List<AreaSummary> GetAreaSummaries()
        {
            var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var open = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (Issue issue in _all)
            {
                string area = string.IsNullOrWhiteSpace(issue.Area) ? ServiceAreaCatalog.Unassigned : issue.Area;
                Bump(totals, area);
                if (issue.Status != RequestStatus.Resolved)
                    Bump(open, area);
            }

            var result = new List<AreaSummary>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string area in ServiceAreaCatalog.Areas)
                AddSummary(result, seen, totals, open, area);

            AddSummary(result, seen, totals, open, ServiceAreaCatalog.Unassigned);

            var extras = new List<string>();
            foreach (string area in totals.Keys)
            {
                if (!seen.Contains(area))
                    extras.Add(area);
            }
            extras.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (string area in extras)
                AddSummary(result, seen, totals, open, area);

            return result;
        }

        private static void Bump(Dictionary<string, int> counts, string key)
        {
            int current;
            counts.TryGetValue(key, out current);
            counts[key] = current + 1;
        }

        private static void AddSummary(List<AreaSummary> result, HashSet<string> seen,
            Dictionary<string, int> totals, Dictionary<string, int> open, string area)
        {
            seen.Add(area);

            int total;
            int openCount;
            totals.TryGetValue(area, out total);
            open.TryGetValue(area, out openCount);

            result.Add(new AreaSummary(area, total, openCount));
        }
    }
}