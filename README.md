# Municipal Services Application

A C# .NET Framework Windows Forms application for a South African municipality. It lets citizens report municipal issues, browse local events and announcements, and track the progress of their service requests. This is a three-part Portfolio of Evidence (POE) project.

| Part | Feature | Status |
|------|---------|--------|
| Part 1 | Report Issues | Complete |
| Part 2 | Local Events and Announcements | Complete |
| Final POE | Service Request Status | Complete |

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

### Final POE: Service Request Status
- **Request list** showing every submitted service request with its status
- **Tracking search** to find a request by its unique tracking identifier
- **Date and status filters** to narrow the list
- **Details panel** with the full request information and a progress bar showing how far the request has advanced
- **Urgent queue** showing which requests should be handled next
- **Category tree** grouping requests by category
- **Service areas grid** summarising the requests per service area
- **Road network map** of the service areas and the roads between them, with breadth-first and depth-first exploration and minimum spanning tree (Prim and Kruskal) views
- **Data Structures tab** showing the structures that drive the page
- Requests reported through Report Issues appear here automatically
- Demo data (24 sample requests) is loaded at startup so the page is populated for marking

---

## How to Compile and Run
1. Clone this repository:
   `git clone https://github.com/ThembalethuNdlovu/MunicipalServicesApp.git`
2. Switch to the final POE branch (contains all three parts):
   `git checkout Part-3`
3. Open `MunicipalServicesApp.sln` in Visual Studio (2019 or later recommended).
4. Make sure the project targets **.NET Framework** (4.5 or higher), not .NET Core. The code is written for C# 7.3.
5. Build the solution: **Build > Build Solution** (`Ctrl+Shift+B`).
6. Run the application: press `F5` or click **Start**.

---

## How to Use

### Main Menu
All three options are enabled: **Report Issues**, **Local Events and Announcements** and **Service Request Status**. Each opens its own window. Closing it (or clicking **Back to Main Menu**) returns you to the menu.

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

### Service Request Status
1. From the Main Menu click **Service Request Status**. The page opens with the 24 demo requests plus any you have submitted in this session.
2. **Click a request** in the list to see its details and progress bar.
3. **Track a request**: type its unique tracking identifier into the tracking search box and search. The matching request is selected and its details are shown.
4. **Filter**: use the date and status filters to narrow the list. Clear the filters to see everything again.
5. **Urgent queue**: shows the requests that should be handled next, most urgent first.
6. **Category tree**: expand a category to see the requests that belong to it.
7. **Service areas**: shows a summary of the requests per service area.
8. **Road network**: shows the service areas as a map connected by roads. Choose a starting area and run **BFS** or **DFS** to see the order in which areas are visited. Run **Prim** or **Kruskal** to highlight the minimum spanning tree, the cheapest set of roads that connects every area.
9. **Data Structures** tab: shows the structures behind the page (explained below).
10. Click **Back to Main Menu** to return.

> Note: all data is held in memory, so search history, reports and new requests reset each time the application is closed. The sample events are generated relative to today's date, so there are always upcoming events to browse. The 24 demo service requests are re-created at every start.

---

## Data Structures Used (Part 2)

> In .NET, `Dictionary<TKey, TValue>` is the generic hash table implementation, so the dictionary rows below cover the hash table requirement.

| Data structure | Where it is used | Why it was chosen |
|---|---|---|
| `Dictionary<Guid, LocalEvent>` (hash table) | `EventRepository` | O(1) lookup of an event by its unique ID. |
| `SortedDictionary<DateTime, List<LocalEvent>>` | `EventRepository` | Keeps events grouped by day and always in date order, so listings need no extra sorting. Date-range searches stop as soon as they pass the end date. |
| `Dictionary<string, List<LocalEvent>>` | `EventRepository` | Groups events by category (case-insensitive) and is exposed through `GetByCategory()`. The search page currently filters by category inside `Search()`, so this lookup is available for later parts. |
| `HashSet<string>` (set) | `EventRepository` | Stores the unique categories, which feed the category dropdown without duplicates. |
| `SortedSet<DateTime>` (set) | `EventRepository` | Stores the unique event dates in order, exposed through `GetEventDates()` (available for later parts). |
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

## Data Structures Used (Final POE: Service Request Status)

The Service Request Status page has to hold many requests and answer different questions about them quickly: "find request X", "show requests between two dates", "what should be handled next", "what belongs to this category" and "how are the service areas connected". No single structure answers all of these well, so each question is served by the structure that suits it. `ServiceRequestService` builds and keeps them in step with `IssueRepository`: when a new issue is added or updated, the `IssueAdded` / `IssueUpdated` events refresh every structure.

| Data structure | Where it is used | Question it answers | Typical cost |
|---|---|---|---|
| Binary tree / binary search tree | Base classes for the tree family | Foundation: node structure, ordering rule and traversals | BST: O(log n) average, O(n) worst case |
| **AVL tree** (keyed by `Sequence`) | `ServiceRequestService` | "Find the request with this tracking number" | O(log n) guaranteed |
| **Red-black tree** (keyed by `DateSequenceKey`) | `ServiceRequestService` | "Show requests in date order / within a date range" | O(log n) guaranteed |
| **Binary heap** | `ServiceRequestService` ("next to handle") | "Which request should be handled next?" | Insert/extract O(log n), peek O(1) |
| **General tree** | `ServiceRequestService` | "Which requests belong to each category?" | Insert O(1), walk O(n) |
| **Graph** (weighted, undirected) | `ServiceAreaCatalog`, `Graph<T>` | "How are the service areas connected by road?" | Traversal O(V + E) |
| **BFS / DFS** | `Graph<T>` | "What can be reached from this area, and in what order?" | O(V + E) |
| **Prim / Kruskal** (minimum spanning tree) | `Graph<T>` | "What is the cheapest set of roads that connects every area?" | Prim O(E log V), Kruskal O(E log E) |
| **Disjoint set (union-find)** | `DisjointSet`, used by Kruskal | "Would adding this road create a loop?" | Near-constant per operation |

### Binary tree and binary search tree
A binary tree is a tree in which every node has at most two children. A binary search tree (BST) adds an ordering rule: everything in the left subtree has a smaller key and everything in the right subtree a larger key. Searching then discards half of the remaining tree at every step. These two classes are the foundation that the AVL and red-black trees extend. A plain BST has a weakness: if keys arrive already sorted (as request numbers do, 1, 2, 3, ...) it degenerates into a chain and searching becomes O(n). That is why the two self-balancing trees below are used for the real data.

### AVL tree: finding a request by tracking number
- **Role:** the main lookup index. Requests are keyed by their `Sequence` number, and the tracking search box uses it to find a request.
- **How it works:** an AVL tree is a BST that keeps the heights of every node's two subtrees within 1 of each other. After each insert it checks this balance rule and, when it is broken, repairs it with a left, right, left-right or right-left rotation.
- **Why it helps:** the tree is guaranteed to stay shallow, so a lookup is O(log n) no matter what order requests arrive in.
- **Example:** the 24 demo requests are inserted in order, 1 to 24. In a plain BST that would be a 24-level chain, and finding request 24 would take 24 comparisons. In the AVL tree, rotations keep the tree about 5 levels deep, so any request is found in at most about 5 comparisons. The gap grows as the number of requests grows: for 1 000 requests it is about 10 comparisons instead of up to 1 000.

### Red-black tree: ordering requests by date
- **Role:** keeps requests ordered by date for the date filter and the date-ordered list. The key is `DateSequenceKey`, the request date combined with its sequence number, so two requests submitted on the same day never collide.
- **How it works:** every node is coloured red or black. Rules (the root is black, a red node has no red child, every path from the root to a leaf has the same number of black nodes) keep the longest path at most twice the shortest. Recolouring and rotations restore the rules after each insert.
- **Why it helps:** an in-order walk of the tree returns requests already sorted by date, so the list never needs re-sorting, and a date range can be answered by finding the first matching key and walking forward until the end date is passed. Compared with the AVL tree it rebalances with fewer rotations, which suits a structure that is updated often as new reports arrive.
- **Example:** filtering for "1 November to 10 November" starts at the earliest request on or after 1 November and stops at the first request after 10 November. Requests outside that window are not examined.

### Binary heap: the urgent queue
- **Role:** powers the "next to handle" urgent queue.
- **How it works:** a binary heap is a complete binary tree stored in an array in which every parent is more urgent than its children, so the most urgent request is always at the root. Inserting adds the item at the bottom and sifts it up. Removing takes the root, moves the last item to the top and sifts it down.
- **Why it helps:** reading the most urgent request is O(1) and insert and extract are O(log n). The full list never has to be sorted just to answer "what is next?".
- **Example:** when a new urgent report arrives it is sifted up past less urgent requests in about log2(n) steps and becomes the next request to handle. Handling a request removes the root, and the next most urgent request rises to the top.

### General tree: grouping requests by category
- **Role:** the category tree. The root represents all requests, each child is a category (for example Roads, Sanitation, Utilities) and each category's children are the requests in it.
- **How it works:** unlike a binary tree, a node can have any number of children, so the structure follows the real-world hierarchy.
- **Why it helps:** adding a request to its category is O(1) once the category node is known, and showing a category's requests means reading only that branch instead of scanning every request.
- **Example:** expanding "Roads" in the category tree shows only the road-related requests, with the rest of the tree untouched.

### Graph: the service-area road network
- **Role:** models how the service areas relate to each other. `ServiceAreaCatalog` defines **10 service areas** (vertices) and **17 roads** (weighted, undirected edges). `Graph<T>` is generic, so it can hold any vertex type.
- **How it works:** the graph is stored as an adjacency list: each area keeps a list of its neighbouring areas and the weight of the road to each. This takes space proportional to the number of vertices plus edges, which suits a sparse road network far better than a V x V matrix.
- **Why it helps:** the relationships between areas (which areas border each other, how areas are linked to each other) cannot be expressed in a list or a tree. A graph is the natural structure, and it drives the road network map and the service areas summary.

### BFS and DFS: exploring the network
- **Breadth-first search (BFS)** uses a queue and visits areas in rings: the start area, then all areas one road away, then two roads away, and so on. It shows which areas are closest to a chosen area in number of roads.
- **Depth-first search (DFS)** goes as far as it can along one road before backtracking, which shows how the network can be followed from one end to the other and confirms that every area is reachable.
- **Cost:** both visit each vertex and each road once, so they run in O(V + E). For 10 areas and 17 roads that is at most 27 steps.
- **Example:** starting BFS from one area lists its directly connected neighbours first, then the areas beyond them, so a dispatcher can see which areas are nearest to a depot.

### Minimum spanning tree: Prim and Kruskal
- **Role:** finds the cheapest set of roads that still connects every service area. A spanning tree of 10 areas uses exactly **9 roads**, so out of the 17 roads **8 are left out**. Prim and Kruskal are two different ways to reach the same minimum total weight, and both are shown so they can be compared.
- **Prim's algorithm** grows one tree outwards from a starting area. At each step it adds the cheapest road that joins a new area to the tree. It keeps the candidate roads in a priority queue, so each step is cheap. Cost: O(E log V).
- **Kruskal's algorithm** sorts all roads by weight and takes them from the cheapest up, skipping any road that would create a loop. Cost: O(E log E) for the sort.
- **Why it helps:** it answers "if we could only maintain or service some of the roads, which ones link every area at the lowest total cost?" Checking all possible road combinations would be impractical, while these greedy algorithms find the answer in a fraction of a second.
- **Example:** with 17 roads, Kruskal examines them cheapest first and stops once it has accepted 9. If two roads have the same weight, the two algorithms may pick different roads, but the total weight is always the same.

### Disjoint set (union-find): detecting loops for Kruskal
- **Role:** the helper that makes Kruskal fast. It tracks which areas are already connected.
- **How it works:** each area starts in its own set. `Find` returns the set an area belongs to, and `Union` merges two sets. A road may be added only if its two ends are in different sets (otherwise it would create a loop); after adding it, the two sets are merged.
- **Why it helps:** the loop check is near-constant time, instead of having to search the partially built tree again for every road.
- **Example:** if roads A-B and B-C have been accepted, A, B and C share one set, so a road A-C is rejected immediately because both ends are already in the same set.

### Verification
All of the structures above are covered by `StructureSelfTests` (143 checks, all passing), including the service request tests in `StructureSelfTests.ServiceRequests.cs`. They check things such as balanced height after sequential inserts, correct ordering from the red-black tree, heap order, the 9-edge spanning tree and agreement between Prim and Kruskal.

---

## Project Structure
```
MunicipalServicesApp/
├── Forms/
│   ├── MainMenuForm.cs
│   ├── ReportIssueForm.cs
│   ├── LocalEventsForm.cs
│   ├── ServiceRequestStatusForm.cs
│   └── ServiceRequestStatusForm.Graph.cs
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
│   ├── RecommendationService.cs
│   ├── ServiceRequestService.cs
│   ├── ServiceAreaCatalog.cs
│   └── DemoDataSeeder.cs
├── Structures/
│   ├── BinaryTree.cs
│   ├── BinarySearchTree.cs
│   ├── GeneralTree.cs
│   ├── AvlTree.cs
│   ├── RedBlackTree.cs
│   ├── BinaryHeap.cs
│   ├── DisjointSet.cs
│   └── Graph.cs
├── StructureSelfTests.cs
├── StructureSelfTests.ServiceRequests.cs
└── Program.cs
```

---

## User Engagement Strategy
The application uses a **real-time progress and feedback strategy**. In Report Issues, a progress bar and dynamic label update live as the user fills in the form, giving immediate positive reinforcement and reducing the chance of abandonment. In Local Events, the recommendation panel and rotating announcement bar keep the content relevant and fresh as the user explores. In Service Request Status, a progress bar on each request shows how far it has advanced, so citizens can see that their report is being dealt with. This approach is grounded in research into citizen engagement with municipal e-government platforms (see the accompanying research document for the full justification and references).

---

## Data Storage
Reported issues, events and service requests are stored in memory for the current session through singleton repositories and services (`IssueRepository`, `EventRepository`, `SearchHistoryService`, `ServiceRequestService`). When the application starts, `Program.Main` calls `DemoDataSeeder.Seed()` once to load the 24 demo service requests. Nothing is written to disk, so everything resets when the application closes.

---

## Author
Thembalethu Ndlovu
