using System.Collections.Generic;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Orders service requests for the "next to handle" queue. The request that compares
    /// as smallest is the most urgent:
    ///   1. lower Priority number first (1 = urgent),
    ///   2. then the request that was updated longest ago,
    ///   3. then the lower Sequence number, so the order is always the same.
    /// </summary>
    public class IssueUrgencyComparer : IComparer<Issue>
    {
        public static readonly IssueUrgencyComparer Instance = new IssueUrgencyComparer();

        public int Compare(Issue x, Issue y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            int byPriority = x.Priority.CompareTo(y.Priority);
            if (byPriority != 0) return byPriority;

            int byAge = x.LastUpdated.CompareTo(y.LastUpdated);
            if (byAge != 0) return byAge;

            return x.Sequence.CompareTo(y.Sequence);
        }
    }
}