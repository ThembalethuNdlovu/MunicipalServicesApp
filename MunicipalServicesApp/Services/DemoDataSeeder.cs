using System;
using System.Collections.Generic;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Creates a fixed set of demo service requests so the Service Request Status
    /// page has something to show. The data is deterministic (no randomness), so
    /// the demo behaves the same every run.
    /// </summary>
    public static class DemoDataSeeder
    {
        private static bool _seeded;

        private class SeedRow
        {
            public string Location;
            public string Category;
            public string Description;
            public int DaysAgo;
            public int StatusSteps; // how many times to advance the status (0-3)

            public SeedRow(string location, string category, string description,
                           int daysAgo, int statusSteps)
            {
                Location = location;
                Category = category;
                Description = description;
                DaysAgo = daysAgo;
                StatusSteps = statusSteps;
            }
        }

        // Dates are deliberately mixed (not in order) so the date index has real work to do.
        private static readonly List<SeedRow> Rows = new List<SeedRow>
        {
            new SeedRow("Vilakazi Street, Soweto", "Roads", "Large pothole near the intersection damaging vehicles.", 21, 3),
            new SeedRow("Selborne Avenue, Alexandra", "Sanitation", "Blocked sewer overflowing onto the street.", 9, 2),
            new SeedRow("Rivonia Road, Sandton", "Utilities (Water/Electricity)", "Street lights out for three nights in a row.", 5, 1),
            new SeedRow("Republic Road, Randburg", "Waste Management", "Refuse not collected for two weeks.", 14, 2),
            new SeedRow("Ontdekkers Road, Roodepoort", "Public Safety", "Missing manhole cover on the pavement.", 2, 0),
            new SeedRow("Old Pretoria Road, Midrand", "Roads", "Faded road markings and a broken traffic light.", 27, 3),
            new SeedRow("Lilly Road, Diepsloot", "Utilities (Water/Electricity)", "No water supply since Monday.", 3, 1),
            new SeedRow("Jorissen Street, Braamfontein", "Parks and Recreation", "Playground equipment broken and unsafe.", 18, 2),
            new SeedRow("Golden Highway, Orange Farm", "Sanitation", "Illegal dumping next to the school.", 11, 1),
            new SeedRow("Witkoppen Road, Fourways", "Roads", "Storm-water drain blocked, the road floods when it rains.", 7, 2),
            new SeedRow("Chris Hani Road, Soweto", "Public Safety", "Broken fence at the community clinic.", 1, 0),
            new SeedRow("13th Avenue, Alexandra", "Waste Management", "Overflowing communal bins attracting rats.", 24, 3),
            new SeedRow("Grayston Drive, Sandton", "Utilities (Water/Electricity)", "Burst water pipe flooding the pavement.", 4, 2),
            new SeedRow("Hendrik Potgieter Road, Roodepoort", "Parks and Recreation", "Overgrown park, grass not cut for months.", 16, 1),
            new SeedRow("Beyers Naude Drive, Randburg", "Roads", "Road surface collapsing near the bridge.", 8, 1),
            new SeedRow("Allandale Road, Midrand", "Sanitation", "Public toilets closed and unclean.", 12, 2),
            new SeedRow("Extension 1, Diepsloot", "Public Safety", "Exposed electrical cables near a children's play area.", 0, 0),
            new SeedRow("Empire Road, Braamfontein", "Other", "Request for additional bus shelter signage.", 30, 3),
            new SeedRow("Extension 4, Orange Farm", "Waste Management", "Skip bins needed for weekly collection.", 19, 2),
            new SeedRow("Cedar Road, Fourways", "Utilities (Water/Electricity)", "Frequent power outages in the evening.", 6, 1),
            new SeedRow("Mofolo Road, Soweto", "Sanitation", "Stagnant water pooling in the open drain.", 13, 2),
            new SeedRow("Katherine Street, Sandton", "Roads", "Speed bump needed outside the primary school.", 22, 3),
            new SeedRow("Main Reef Road, Roodepoort", "Public Safety", "Streetlight pole leaning dangerously.", 10, 1),
            new SeedRow("Louis Botha Avenue, Alexandra", "Other", "Noise complaint about an illegal late-night venue.", 15, 2)
        };

        /// <summary>
        /// Adds the demo requests once per session. Returns how many were added
        /// (0 if they were already loaded).
        /// </summary>
        public static int Seed()
        {
            if (_seeded)
                return 0;
            _seeded = true;

            DateTime now = DateTime.Now;
            IssueRepository repo = IssueRepository.Instance;

            for (int i = 0; i < Rows.Count; i++)
            {
                SeedRow row = Rows[i];
                DateTime reported = now.AddDays(-row.DaysAgo).AddHours(-(i * 5 % 20));
                if (reported > now) reported = now;

                var issue = new Issue(row.Location, row.Category, row.Description, reported);

                // Replay the status changes at plausible times after the report date
                for (int step = 1; step <= row.StatusSteps; step++)
                {
                    DateTime when = reported.AddDays(step * 1.5);
                    if (when > now) when = now;
                    issue.AdvanceStatus(when);
                }

                repo.AddIssue(issue);
            }

            return Rows.Count;
        }
    }
}