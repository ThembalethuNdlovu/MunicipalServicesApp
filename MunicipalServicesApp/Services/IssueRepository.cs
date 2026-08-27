using System.Collections.Generic;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Central store for all reported issues. Acts as the single source of truth
    /// so that future features (Part 2/3) can build on top of this data without
    /// changing how issues are captured here.
    /// </summary>
    public class IssueRepository
    {
        // Simple singleton pattern: one shared instance for the whole app session.
        private static readonly IssueRepository _instance = new IssueRepository();
        public static IssueRepository Instance => _instance;

        private readonly List<Issue> _issues;

        private IssueRepository()
        {
            _issues = new List<Issue>();
        }

        public void AddIssue(Issue issue)
        {
            _issues.Add(issue);
        }

        public List<Issue> GetAllIssues()
        {
            return _issues;
        }

        public int Count => _issues.Count;
    }
}