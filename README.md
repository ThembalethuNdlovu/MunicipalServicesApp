# Municipal Services Application — Part 1 (Report Issues)

## Overview
This is a C# .NET Framework Windows Forms application developed for a South African municipality to allow citizens to report municipal issues (e.g. sanitation, roads, utilities). This is Part 1 of a three-part Portfolio of Evidence project.

## Features Implemented (Part 1)
- **Main Menu** with three options: Report Issues (active), Local Events and Announcements (disabled — to be implemented in Part 2), Service Request Status (disabled — to be implemented in the final POE).
- **Report Issues form**:
  - Location input
  - Category selection (dropdown)
  - Description (rich text box)
  - Media attachment support (images/documents via file dialog, multiple files supported)
  - Real-time engagement progress bar with encouraging messages, updating live as the user completes the form
  - Form validation with clear error messages
  - Success confirmation on submission
  - Navigation back to the main menu

## User Engagement Strategy
This application implements a **real-time progress and feedback strategy**: a progress bar and dynamic label update live as the user fills in the Report Issues form, providing immediate positive reinforcement and reducing the likelihood of form abandonment. This approach is grounded in research into citizen engagement with municipal e-government platforms (see accompanying research document for full justification and references).

## How to Compile and Run
1. Clone this repository: `git clone https://github.com/ThembalethuNdlovu/MunicipalServicesApp.git`
2. Open `MunicipalServicesApp.sln` in Visual Studio (2019 or later recommended).
3. Ensure the project is targeting **.NET Framework** (not .NET Core).
4. Build the solution: `Build → Build Solution` (or `Ctrl+Shift+B`).
5. Run the application: press `F5` or click **Start**.

## How to Use
1. On launch, the Main Menu appears.
2. Click **Report Issues**.
3. Fill in the Location, select a Category, and provide a Description.
4. Watch the progress bar and label update as you complete each field.
5. Optionally click **Attach Media** to attach one or more images/documents relevant to the issue.
6. Click **Submit** to save the report. A confirmation message will appear, and the form will reset for a new report.
7. Click **Back to Main Menu** to return to the main screen.

## Project Structure
MunicipalServicesApp/
├── Forms/
│ ├── MainMenuForm.cs
│ └── ReportIssueForm.cs
├── Models/
│ └── Issue.cs
├── Services/
│ └── IssueRepository.cs
└── Program.cs


## Data Storage
Reported issues are stored in-memory for the current application session via a singleton `IssueRepository`, using a `List<Issue>`. This will be extended in Part 2 and the final POE with additional data structures (dictionaries, sets, trees, etc.) for advanced features.

## Author
Thembalethu Ndlovu