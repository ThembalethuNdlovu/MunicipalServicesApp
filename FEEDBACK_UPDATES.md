# Updates Made Based on Lecturer Feedback

**Project:** Municipal Services Application (C# Windows Forms, .NET Framework)
**Student:** Thembalethu Ndlovu

## Feedback received (Part 2: Local Events and Announcements)

Part 2 was marked 97/100. The lecturer suggested that the recommendation
feature should take **search recency** and **date preferences** into account
when scoring events, instead of relying on how often a category or keyword has
been searched.

## Update 1: Recency-weighted recommendations

**Commit:** `a63d50c` - "Recommendations: add search recency decay and date-preference scoring (lecturer feedback)"

**Files changed:** `Services/SearchHistoryService.cs`, `Services/RecommendationService.cs`

**What was wrong before:** every search counted equally forever. A category
searched three times last month outranked one searched an hour ago.

**What changed:**

- `SearchHistoryService` now stores every search with a timestamp in a new
  timeline (`SearchEntry`). `RecordSearch` has an overload that accepts an
  explicit timestamp, and `GetTimeline()` returns a copy of the timeline.
  `Clear()` also clears the timeline.
- `RecommendationService` weights each search by an exponential decay:
  `weight = 0.5 ^ (age in hours / HalfLifeHours)`. A search made just now has
  weight 1.0, and a search loses half its influence every `HalfLifeHours`
  (default 24).
- Category and keyword scores are now the sum of these recency weights instead
  of raw counts.

## Update 2: Date preferences

**Commit:** `a63d50c` (same commit as above)

**Files changed:** `Services/SearchHistoryService.cs`, `Services/RecommendationService.cs`

**What changed:**

- The From/To range the user searches by is now treated as a preferred date
  window.
- Events inside a preferred window receive the full date bonus. The bonus fades
  linearly to zero for events up to 14 days outside the window (`DateRangeDays`).
- The date bonus is also multiplied by the search's recency weight, so recent
  date preferences matter more than old ones.
- Only the best bonus across all windows is used (not the sum), so repeated date
  searches cannot overwhelm the category and keyword signals.
- Searches with dates but no keyword or category are now recorded for date
  preferences only. They do not appear in the "Recent searches" label and do
  not change the keyword or category counts.
- Recommendation reasons now explain date matches, for example "Happens within
  the dates you searched".

## Scoring summary

| Signal | Contribution |
|---|---|
| Category match | recency-weighted searches x 3.0 |
| Keyword match | recency-weighted searches x 2.0 |
| Date preference | 4.0 x recency x proximity (1.0 inside the window, fading to 0 at 14 days) |
| Event priority | 6 - priority |

Scores are fractional internally and are multiplied by 10 and rounded before
being stored in `EventRecommendation`, so that class did not need to change.
The weights and half-life are named constants at the top of
`RecommendationService.cs`.

## Files not changed

`LocalEvent.cs`, `LocalEventsForm.cs`, `EventRepository.cs` and
`EventRecommendation` were left as they were. The form already passed the
From/To dates into `RecordSearch`.

## Testing performed

Tick each item once you have run it:

- [ ] Empty history shows featured events with no errors
- [ ] A more recent search outranks an older, more frequent one (tested with a shortened half-life, then restored to 24 hours)
- [ ] Events inside a searched date window are recommended with a date reason
- [ ] Events a few days outside the window rank lower than events inside it
- [ ] Repeating an identical search does not reshuffle the order
- [ ] A date-only search does not add an entry to "Recent searches"
