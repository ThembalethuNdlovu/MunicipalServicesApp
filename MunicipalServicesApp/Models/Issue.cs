using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// Represents a single citizen-reported municipal issue.
    /// </summary>
    public class Issue
    {
        public Guid Id { get; private set; }
        public string Location { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public List<string> AttachmentPaths { get; private set; }
        public DateTime DateReported { get; private set; }

        public Issue(string location, string category, string description)
        {
            Id = Guid.NewGuid();
            Location = location;
            Category = category;
            Description = description;
            AttachmentPaths = new List<string>();
            DateReported = DateTime.Now;
        }

        public void AddAttachment(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                AttachmentPaths.Add(filePath);
            }
        }
    }
}