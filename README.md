# Municipal Services Application

A C# .NET Framework Windows Forms application for a South African municipality. It lets citizens report municipal issues and browse local events and announcements. This is a three-part Portfolio of Evidence (POE) project.

| Part | Feature | Status |
|------|---------|--------|
| Part 1 | Report Issues | Complete |
| Part 2 | Local Events and Announcements | Complete |
| Final POE | Service Request Status | To be implemented |

---

## Features

### Part 1: Report Issues
- Location input
- Category selection (dropdown)
- Description (rich text box)
- Media attachment support (images/documents via file dialog, multiple files supported)
- Real-time engagement progress bar with encouraging messages, updating live as the user completes the form
- Form validation with clear error messages
- Success confirmation on submission
- Navigation back to the main menu

### Part 2: Local Events and Announcements
- **Announcement bar** at the top of the page that rotates through municipal announcements every 5 seconds
- **Event list** showing date, title, category and location, ordered by date (announcements are tagged `[Notice]` and highlighted)
- **Search** by keyword, category and an optional date range (From / To)
- **Featured** button showing the most important upcoming events (lowest priority number first)
- **Event details panel** with full information for the selected event
- **Recommended for you** panel that suggests events based on the user's previous searches and explains why each one is suggested
- **Recent searches** line showing the last three searches
- **Clear** button to reset all filters
- Responsive layout that resizes with the window (minimum size 980 x 640)
- Input validation with message boxes (e.g. "From" date later than "To" date) and on-screen feedback when no events match

---

## How to Compile and Run
1. Clone this repository:
   `git clone https://github.com/ThembalethuNdlovu/MunicipalServicesApp.git`
2. To work on Part 2, switch to the Part 2 branch:
   `git checkout part-2-local-events`
3. Open `MunicipalServicesApp.sln` in Visual Studio (2019 or later recommended).
4. Make sure the project targets **.NET Framework** (4.5 or higher), not .NET Core.
5. Build the solution: **Build > Build Solution** (`Ctrl+Shift+B`).
6. Run the application: press `F5` or click **Start**.

---

## How to Use

### Report Issues
1. On launch the Main Menu appears. Click **Report Issues**.
2. Fill in the Location, select a Category and provide a Description.
3. Watch the progress bar and label update as you complete each field.
4. Optionally click **Attach Media** to attach one or more images or documents.
5. Click **Submit**. A confirmation message appears and the form resets for a new report.
6. Click **Back to Main Menu** to return.

### Local Events and Announcements
1. From the Main Menu click **Local Events and Announcements**.
2. The page opens with all upcoming events. The yellow bar at the top cycles through current announcements.
3. **Click any event** in the list to read its full details on the right.
4. **To search**, use any combination of:
   - **Keyword**: matches the event title, description or location (e.g. `clinic`)
   - **Category**: choose a category from the dropdown (or leave on "All Categories")
   - **From / To**: tick the checkbox next to a date to enable it, then pick the date
   
   Then click **Search** (or press Enter). Searches show upcoming events only, unless you choose an earlier From date.
5. Click **Featured** to see the highest-priority upcoming events.
6. Click **Clear** to reset the filters and show all upcoming events.
7. **Recommendations**: the more you search, the more the **Recommended for you** panel adapts. Click a recommendation to see its details and the reason it was suggested.
8. Click **Back to Main Menu** to return.

> Note: all data is held in memory, so search history and reports reset each time the application is closed. The sample events are generated relative to today's date, so there are always upcoming events to browse.

---

## Data Structures Used (Part 2)

| Data structure | Where it is used | Why it was chosen |
|---|---|---|
| `Dictionary<Guid, LocalEvent>` (hash table) | `EventRepository` | O(1) lookup of an event by its unique ID. |
| `SortedDictionary<DateTime, List<LocalEvent>>` | `EventRepository` | Keeps events grouped by day and always in date order, so listings need no extra sorting. Date-range searches stop as soon as they pass the end date. |
| `Dictionary<string, List<LocalEvent>>` | `EventRepository` | Groups events by category (case-insensitive) for quick category retrieval. |
| `HashSet<string>` (set) | `EventRepository` | Stores the unique categories, which feed the category dropdown without duplicates. |
| `SortedSet<DateTime>` (set) | `EventRepository` | Stores the unique event dates in order. |
| `Queue<LocalEvent>` | `EventRepository` | Announcements are dequeued from the front and re-enqueued at the back, so the announcement bar cycles fairly (first in, first out). |
| `MinPriorityQueue<T>` (custom binary min-heap) | `EventRepository`, `RecommendationService` | .NET Framework has no built-in priority queue, so one was written. It picks the top featured events (priority number, then date) and the top-scoring recommendations. Enqueue and dequeue are O(log n). |
| `Stack<SearchRecord>` | `SearchHistoryService` | Records searches with the most recent on top (last in, first out), used for the "Recent searches" line. |
| `Dictionary<string, int>` (x2) | `SearchHistoryService` | Counts how often each category and keyword has been searched. This is the user's interest profile. |

### Recommendation Algorithm
The recommendation feature (`RecommendationService`) analyses the user's search patterns:

1. Every search with a keyword or category is pushed onto the history stack, and the category and keyword counters are incremented.
2. Each upcoming event receives a score:
   - **Category match**: `times that category was searched x 3`
   - **Keyword match**: `times that keyword was searched x 2` for each searched keyword found in the event's title, description or location
   - **Priority bonus**: `6 - priority`, so more important events rank slightly higher
3. Events with a score of zero are ignored. The rest go into the priority queue, which returns the highest scores first (earliest date breaks ties).
4. If fewer than five events qualify (for example before the user has searched anything), the list is topped up with featured events so the panel is never empty.
5. Each recommendation shows a plain-language reason, such as "You often search Health events".

---

## Project Structure
```
MunicipalServicesApp/
├── Forms/
│   ├── MainMenuForm.cs
│   ├── ReportIssueForm.cs
│   └── LocalEventsForm.cs
├── Models/
│   ├── Issue.cs
│   ├── LocalEvent.cs
│   ├── SearchRecord.cs
│   └── EventRecommendation.cs
├── Services/
│   ├── IssueRepository.cs
│   ├── EventRepository.cs
│   ├── MinPriorityQueue.cs
│   ├── SearchHistoryService.cs
│   └── RecommendationService.cs
└── Program.cs
```

---

## User Engagement Strategy
The application uses a **real-time progress and feedback strategy**. In Report Issues, a progress bar and dynamic label update live as the user fills in the form, giving immediate positive reinforcement and reducing the chance of abandonment. In Local Events, the recommendation panel and rotating announcement bar keep the content relevant and fresh as the user explores. This approach is grounded in research into citizen engagement with municipal e-government platforms (see the accompanying research document for the full justification and references).

---

## Data Storage
Reported issues and events are stored in memory for the current session through singleton repositories (`IssueRepository`, `EventRepository`, `SearchHistoryService`). Part 3 (Service Request Status) will extend this with trees, heaps and graphs.

---

## Author
Thembalethu Ndlovu
