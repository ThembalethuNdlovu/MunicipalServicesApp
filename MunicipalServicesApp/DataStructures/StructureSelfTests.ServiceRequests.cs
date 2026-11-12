using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Services;

namespace MunicipalServicesApp.DataStructures
{
    public static partial class StructureSelfTests
    {
        private static void TestServiceRequestService(Report r)
        {
            // ---------------------------------------------------------------
            r.Heading("Service request facade: empty");

            var empty = new ServiceRequestService(null);
            r.Check("Empty facade: count 0, no next request, nothing found",
                empty.Count == 0 && empty.OpenCount == 0 && empty.NextToHandle == null &&
                empty.FindByTrackingNumber("SR-000001") == null &&
                empty.GetAllRequests().Count == 0 &&
                empty.GetByDateRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)).Count == 0 &&
                empty.GetUrgentQueue(5).Count == 0);

            List<AreaSummary> emptySummaries = empty.GetAreaSummaries();
            r.Check("Empty facade: area summaries list every area plus Unassigned, all zero",
                emptySummaries.Count == ServiceAreaCatalog.Areas.Count + 1 &&
                emptySummaries.All(s => s.Total == 0 && s.Open == 0));

            // ---------------------------------------------------------------
            r.Heading("Service request facade: 12 requests indexed in scrambled order");

            string[] locations =
            {
                "Vilakazi St, Soweto", "Rivonia Rd, Sandton", "Jan Smuts Ave, Randburg", "Main Rd, Alexandra",
                "Beyers Naude Dr, Randburg", "Grayston Dr, Sandton", "Cedar Rd, Fourways", "Old Pretoria Rd, Midrand",
                "Hendrik Potgieter, Roodepoort", "11 Unknown Lane", "Kingsway, Braamfontein", "Soweto Highway, Soweto"
            };
            string[] categories = { "Roads", "Public Safety", "Sanitation", "Parks and Recreation" };

            // Requests 10 and 11 are reported at exactly the same moment
            DateTime baseDate = new DateTime(2026, 11, 1, 8, 0, 0);
            var issues = new List<Issue>();
            for (int i = 0; i < 12; i++)
            {
                DateTime reported = baseDate.AddDays(i == 11 ? 10 : i);
                issues.Add(new Issue(locations[i], categories[i % 4], "Test request " + i, reported));
            }

            var service = new ServiceRequestService(null);
            bool allIndexed = true;
            for (int i = 0; i < 12; i++)
            {
                // 5 and 12 share no factors, so this visits every index 0..11 once, out of order
                if (!service.Index(issues[(i * 5) % 12]))
                    allIndexed = false;
            }

            r.Check("All 12 requests were indexed", allIndexed && service.Count == 12);
            r.Check("Indexing the same request again is rejected and changes nothing",
                !service.Index(issues[3]) && service.Count == 12 && service.OpenCount == 12);
            r.Check("AVL, red-black and heap structures are all valid",
                service.SequenceIndex.Validate() && service.DateIndex.Validate() &&
                service.UrgencyHeap.Validate());

            // --- tracking-number lookup (AVL)
            Issue target = issues[7];
            r.Check("Find by tracking number (SR-......)", service.FindByTrackingNumber(target.TrackingNumber) == target);
            r.Check("Find works in lower case", service.FindByTrackingNumber(target.TrackingNumber.ToLowerInvariant()) == target);
            r.Check("Find works with just the number",
                service.FindByTrackingNumber(target.Sequence.ToString()) == target);
            r.Check("Find needs no more comparisons than the AVL tree height",
                service.LastLookupComparisons >= 1 && service.LastLookupComparisons <= service.SequenceIndex.Height);
            r.Check("A tracking number that does not exist returns null",
                service.FindByTrackingNumber("SR-999999") == null);
            r.Check("Text that is not a tracking number returns null with 0 comparisons",
                service.FindByTrackingNumber("abc") == null && service.LastLookupComparisons == 0 &&
                service.FindByTrackingNumber("") == null && service.FindByTrackingNumber(null) == null);
            r.Check("GetAllRequests returns every request in tracking-number order",
                SrSequences(service.GetAllRequests()) == SrSequences(issues));

            // --- date range (red-black)
            r.Check("Date range 3-5 Nov (whole days) gives requests 2,3,4",
                SrSequences(service.GetByDateRange(new DateTime(2026, 11, 3), new DateTime(2026, 11, 5))) ==
                SrSequences(new[] { issues[2], issues[3], issues[4] }));
            r.Check("Two requests reported at the same moment are both found (11 Nov gives 10 and 11)",
                SrSequences(service.GetByDateRange(new DateTime(2026, 11, 11), new DateTime(2026, 11, 11))) ==
                SrSequences(new[] { issues[10], issues[11] }));
            r.Check("Date range covering everything gives all 12",
                service.GetByDateRange(new DateTime(2026, 10, 1), new DateTime(2026, 12, 31)).Count == 12);
            r.Check("Date range before all requests, or with from > to, gives nothing",
                service.GetByDateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)).Count == 0 &&
                service.GetByDateRange(new DateTime(2026, 11, 9), new DateTime(2026, 11, 3)).Count == 0);

            // --- service areas
            r.Check("Areas were assigned from the location text",
                issues[0].Area == "Soweto" && issues[1].Area == "Sandton" &&
                issues[3].Area == "Alexandra" && issues[9].Area == "Unassigned" &&
                issues.All(i => i.Area == ServiceAreaCatalog.FindArea(i.Location)));

            List<AreaSummary> summaries = service.GetAreaSummaries();
            AreaSummary soweto = summaries.FirstOrDefault(s => s.Area == "Soweto");
            AreaSummary sandton = summaries.FirstOrDefault(s => s.Area == "Sandton");
            AreaSummary unassigned = summaries.FirstOrDefault(s => s.Area == "Unassigned");
            AreaSummary diepsloot = summaries.FirstOrDefault(s => s.Area == "Diepsloot");
            r.Check("Area summaries: Soweto 2, Sandton 2, Unassigned 1, Diepsloot 0, total 12",
                soweto != null && soweto.Total == 2 && soweto.Open == 2 &&
                sandton != null && sandton.Total == 2 && sandton.Open == 2 &&
                unassigned != null && unassigned.Total == 1 &&
                diepsloot != null && diepsloot.Total == 0 &&
                summaries.Sum(s => s.Total) == 12);

            // --- category tree
            r.Check("Category tree: 1 root + 4 categories + 12 requests = 17 nodes, 3 levels",
                service.CategoryTree.Count == 17 && service.CategoryTree.Height == 3);
            r.Check("Requests in category 'Roads' are 0, 4 and 8",
                SrSequences(service.GetRequestsInCategory("Roads")) ==
                SrSequences(new[] { issues[0], issues[4], issues[8] }));
            r.Check("Category lookup is not case-sensitive and an unknown category gives nothing",
                service.GetRequestsInCategory("roads").Count == 3 &&
                service.GetRequestsInCategory("Parks").Count == 0);
            r.Check("A request's tree text shows its tracking number, status and location",
                service.CategoryTree.Find(e => e.Issue == issues[1]).Value.ToString() ==
                issues[1].TrackingNumber + " - Submitted - " + issues[1].Location);

            // --- heap: next to handle
            r.Heading("Service request facade: next to handle (heap)");

            // Public Safety (priority 1) requests are 1, 5 and 9; request 1 is the oldest
            r.Check("Next to handle is the oldest Public Safety request (1)", service.NextToHandle == issues[1]);
            r.Check("Urgent queue of 3 is requests 1, 5, 9",
                SrSequences(service.GetUrgentQueue(3)) == SrSequences(new[] { issues[1], issues[5], issues[9] }));
            r.Check("Reading the urgent queue does not change the heap",
                service.OpenCount == 12 && service.UrgencyHeap.Validate() && service.NextToHandle == issues[1]);

            int rebuildsBefore = service.HeapRebuildCount;
            issues[1].AdvanceStatus(baseDate.AddDays(30));
            service.Refresh(issues[1]);
            r.Check("After request 1 is updated, the heap is rebuilt and request 5 is next",
                service.HeapRebuildCount == rebuildsBefore + 1 &&
                service.UrgencyHeap.Validate() && service.NextToHandle == issues[5] &&
                service.OpenCount == 12);

            issues[5].AdvanceStatus(baseDate.AddDays(31));
            issues[5].AdvanceStatus(baseDate.AddDays(32));
            issues[5].AdvanceStatus(baseDate.AddDays(33));
            service.Refresh(issues[5]);
            r.Check("A resolved request leaves the heap: 11 open, next is request 9",
                issues[5].Status == RequestStatus.Resolved &&
                service.OpenCount == 11 && service.Count == 12 &&
                service.UrgencyHeap.Validate() && service.NextToHandle == issues[9]);

            AreaSummary sandtonAfter = service.GetAreaSummaries().FirstOrDefault(s => s.Area == "Sandton");
            r.Check("Area open count drops when a request is resolved (Sandton: 1 open of 2)",
                sandtonAfter != null && sandtonAfter.Total == 2 && sandtonAfter.Open == 1);
            r.Check("The resolved request is still found by its tracking number",
                service.FindByTrackingNumber(issues[5].TrackingNumber) == issues[5]);

            // ---------------------------------------------------------------
            r.Heading("Service request facade: edge cases");

            var edge = new ServiceRequestService(null);

            var noCategory = new Issue("Somewhere, Soweto", null, "No category given", baseDate);
            var presetArea = new Issue("Nowhere in particular", "Roads", "Area already chosen", baseDate.AddDays(1));
            presetArea.Area = "Midrand";
            edge.Index(noCategory);
            edge.Index(presetArea);

            r.Check("A request with no category is filed under 'Other'",
                edge.GetRequestsInCategory("Other").Count == 1 && edge.GetRequestsInCategory(null).Count == 1);
            r.Check("An area that was already assigned is kept",
                presetArea.Area == "Midrand" && noCategory.Area == "Soweto");

            var late = new Issue("Cedar Rd, Fourways", "Sanitation", "Never indexed before", baseDate.AddDays(2));
            edge.Refresh(late);
            r.Check("Refresh on a request that was never indexed indexes it",
                edge.Count == 3 && edge.FindByTrackingNumber(late.TrackingNumber) == late);

            bool nullRejected = false;
            try { edge.Index(null); }
            catch (ArgumentNullException) { nullRejected = true; }
            r.Check("A null request is rejected with ArgumentNullException", nullRejected);

            // ---------------------------------------------------------------
            r.Heading("Service request facade: 500 requests (sequence numbers arrive already sorted)");

            var big = new ServiceRequestService(null);
            var bigIssues = new List<Issue>();
            for (int i = 0; i < 500; i++)
            {
                var issue = new Issue("Road " + i + ", Sandton", "Roads", "Load test " + i, baseDate.AddMinutes(i));
                bigIssues.Add(issue);
                big.Index(issue);
            }

            Issue last = bigIssues[499];
            bool foundLast = big.FindByTrackingNumber(last.TrackingNumber) == last;
            int lookupComparisons = big.LastLookupComparisons;
            int avlHeight = big.SequenceIndex.Height;
            int rbHeight = big.DateIndex.Height;

            r.Check("500 requests: all three structures are valid and the count is 500",
                big.Count == 500 && big.SequenceIndex.Validate() && big.DateIndex.Validate() &&
                big.UrgencyHeap.Validate() && big.OpenCount == 500);
            r.Check("Finding the newest request needs at most 13 comparisons (a linear scan needs 500)",
                foundLast && lookupComparisons <= 13);
            r.Check("Whole-day range 1 Nov gives all 500 requests",
                big.GetByDateRange(baseDate.Date, baseDate.Date).Count == 500);
            r.Note(string.Format("500 requests: AVL height {0}, red-black height {1}, lookup of the newest took {2} comparisons",
                avlHeight, rbHeight, lookupComparisons));
        }

        private static string SrSequences(IEnumerable<Issue> issues)
        {
            return string.Join(",", issues.Select(i => i.Sequence));
        }
    }
}