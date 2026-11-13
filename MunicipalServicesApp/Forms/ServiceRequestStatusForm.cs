using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MunicipalServicesApp.DataStructures;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Services;

namespace MunicipalServicesApp.Forms
{
    /// <summary>
    /// Service Request Status page. Built entirely in code (no Designer file).
    /// Every list and lookup is served by ServiceRequestService, which keeps the
    /// AVL, red-black, heap and tree indexes in step with the IssueRepository.
    /// </summary>
    [DesignerCategory("Code")]
    public partial class ServiceRequestStatusForm : Form
    {
        
        private static readonly Color Brand = Color.FromArgb(30, 120, 80);
        private static readonly Color PageColour = Color.FromArgb(240, 244, 248);

        private static readonly Color GoodColour = Color.FromArgb(0, 110, 60);
        private static readonly Color BadColour = Color.Firebrick;

        private readonly IssueRepository _repository = IssueRepository.Instance;
        private readonly ServiceRequestService _service;

        private Issue _focusedIssue;          // the request shown in the details panel
        private bool _suspendEvents;          // true while the code (not the user) changes controls
        private bool _dateRangeInitialised;
        private bool _lookupDemoShown;
        private bool _categoryTreeBuilt;

        // ---- controls
        private Label _lblSummary;
        private Label _lblFindResult;
        private Label _lblFilterNote;
        private Label _lblNext;
        private Label _lblProgress;
        private Label _lblStatusBar;
        private TextBox _txtTracking;
        private TextBox _txtDetails;
        private TextBox _txtStructure;
        private Button _btnFind;
        private Button _btnClearFilters;
        private Button _btnAdvance;
        private Button _btnHandleNext;
        private Button _btnLookupDemo;
        private Button _btnBack;
        private CheckBox _chkDate;
        private DateTimePicker _dtFrom;
        private DateTimePicker _dtTo;
        private ComboBox _cmbStatus;
        private ComboBox _cmbStructure;
        private DataGridView _gridRequests;
        private DataGridView _gridAreas;
        private ListView _listUrgent;
        private TreeView _treeCategories;
        private ProgressBar _barProgress;
        private Panel _areaGraphHost;
        private SplitContainer _split;

        
        partial void BuildAreaGraphPanel(Panel host);
        partial void RefreshAreaGraphPanel();

        public ServiceRequestStatusForm()
        {
            // Created BEFORE this form subscribes below, so the service always updates
            // its indexes first when the repository raises an event.
            _service = ServiceRequestService.Instance;

            BuildInterface();

            Load += OnFormLoad;
            Shown += OnFormShown;
            FormClosed += OnFormClosed;

            _repository.IssueAdded += OnRepositoryChanged;
            _repository.IssueUpdated += OnRepositoryChanged;
        }

        // =====================================================================
        //  Form events
        // =====================================================================
        private void OnFormLoad(object sender, EventArgs e)
        {
            RefreshAll();
            ShowDetails(null);
            SetStatus("Ready. Search by tracking number, filter the list, or open a tab below.");
        }

        private void OnFormShown(object sender, EventArgs e)
        {
            int distance = _split.Height * 52 / 100;
            if (distance >= _split.Panel1MinSize &&
                _split.Height - distance >= _split.Panel2MinSize + _split.SplitterWidth)
            {
                _split.SplitterDistance = distance;
            }
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            _repository.IssueAdded -= OnRepositoryChanged;
            _repository.IssueUpdated -= OnRepositoryChanged;
        }

        private void OnRepositoryChanged(Issue issue)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
                BeginInvoke(new Action(RefreshAll));
            else
                RefreshAll();
        }

        // =====================================================================
        //  Building the interface
        // =====================================================================
        private void BuildInterface()
        {
            Text = "Service Request Status";
            Font = new Font("Segoe UI", 9.5f);
            BackColor = PageColour;
            MinimumSize = new Size(1000, 680);
            Size = new Size(1200, 780);
            StartPosition = FormStartPosition.CenterScreen;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFilterBar(), 0, 1);
            root.Controls.Add(BuildMainArea(), 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);

            Controls.Add(root);
        }

        private Control BuildHeader()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 8)
            };

            var title = new Label
            {
                Text = "Service Request Status",
                Font = new Font(Font.FontFamily, 18f, FontStyle.Bold),
                ForeColor = Brand,
                AutoSize = true,
                Margin = new Padding(0)
            };

            _lblSummary = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(2, 2, 0, 0)
            };

            panel.Controls.Add(title, 0, 0);
            panel.Controls.Add(_lblSummary, 0, 1);
            return panel;
        }

        private Control BuildFilterBar()
        {
            var group = new GroupBox
            {
                Text = "Find and filter",
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 4, 8, 8),
                Margin = new Padding(0, 0, 0, 8)
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true
            };

            flow.Controls.Add(MakeFieldLabel("Tracking number:"));
            _txtTracking = new TextBox { Width = 140, Margin = new Padding(2, 4, 2, 2) };
            _txtTracking.KeyDown += TxtTracking_KeyDown;
            flow.Controls.Add(_txtTracking);

            _btnFind = MakeButton("Find request", 110);
            _btnFind.Click += (s, e) => FindRequest();
            flow.Controls.Add(_btnFind);

            _chkDate = new CheckBox
            {
                Text = "Filter by date reported",
                AutoSize = true,
                Margin = new Padding(26, 7, 4, 0)
            };
            _chkDate.CheckedChanged += ChkDate_CheckedChanged;
            flow.Controls.Add(_chkDate);

            flow.Controls.Add(MakeFieldLabel("From"));
            _dtFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 110,
                Enabled = false,
                Value = DateTime.Today.AddDays(-30),
                Margin = new Padding(2, 4, 2, 2)
            };
            _dtFrom.ValueChanged += DateFilter_Changed;
            flow.Controls.Add(_dtFrom);

            flow.Controls.Add(MakeFieldLabel("to"));
            _dtTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 110,
                Enabled = false,
                Value = DateTime.Today,
                Margin = new Padding(2, 4, 2, 2)
            };
            _dtTo.ValueChanged += DateFilter_Changed;
            flow.Controls.Add(_dtTo);

            flow.Controls.Add(MakeFieldLabel("Status:"));
            _cmbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Margin = new Padding(2, 4, 2, 2)
            };
            _cmbStatus.Items.Add("All statuses");
            for (int i = 0; i <= 3; i++)
                _cmbStatus.Items.Add(Issue.GetStatusText((RequestStatus)i));
            _cmbStatus.SelectedIndex = 0;
            _cmbStatus.SelectedIndexChanged += DateFilter_Changed;
            flow.Controls.Add(_cmbStatus);

            _btnClearFilters = MakeButton("Clear filters", 100);
            _btnClearFilters.Click += (s, e) => ClearFilters();
            flow.Controls.Add(_btnClearFilters);

            var messages = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown
            };
            _lblFindResult = new Label
            {
                AutoSize = true,
                Margin = new Padding(4, 4, 0, 0),
                Text = "Tip: type SR-000012 or just 12, then press Enter."
            };
            _lblFindResult.ForeColor = Color.DimGray;
            _lblFilterNote = new Label
            {
                AutoSize = true,
                Margin = new Padding(4, 2, 0, 0),
                ForeColor = BadColour
            };
            messages.Controls.Add(_lblFindResult);
            messages.Controls.Add(_lblFilterNote);

            table.Controls.Add(flow, 0, 0);
            table.Controls.Add(messages, 0, 1);
            group.Controls.Add(table);
            return group;
        }

        private Control BuildMainArea()
        {
            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 150,
                Panel2MinSize = 150,
                BackColor = PageColour
            };

            _split.Panel1.Controls.Add(BuildRequestsPane());
            _split.Panel2.Controls.Add(BuildTabs());
            return _split;
        }

        private Control BuildRequestsPane()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _gridRequests = CreateGrid();
            AddColumn(_gridRequests, "colTracking", "Tracking no.", 14f, null);
            AddColumn(_gridRequests, "colCategory", "Category", 20f, null);
            AddColumn(_gridRequests, "colLocation", "Location", 24f, null);
            AddColumn(_gridRequests, "colArea", "Area", 12f, null);
            AddColumn(_gridRequests, "colPriority", "Priority", 8f, null);
            AddColumn(_gridRequests, "colStatus", "Status", 13f, null);
            AddColumn(_gridRequests, "colReported", "Reported", 17f, "yyyy-MM-dd HH:mm");
            AddColumn(_gridRequests, "colUpdated", "Last updated", 17f, "yyyy-MM-dd HH:mm");
            _gridRequests.SelectionChanged += GridRequests_SelectionChanged;
            table.Controls.Add(_gridRequests, 0, 0);

            var group = new GroupBox
            {
                Text = "Selected request",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Margin = new Padding(8, 0, 0, 0)
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _txtDetails = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblProgress = new Label { AutoSize = true, Margin = new Padding(0, 8, 0, 2) };

            _barProgress = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Height = 18,
                Margin = new Padding(0, 0, 0, 8)
            };

            _btnAdvance = MakeButton("Advance status (demo)", 190);
            _btnAdvance.Enabled = false;
            _btnAdvance.Click += (s, e) => AdvanceFocusedIssue();

            inner.Controls.Add(_txtDetails, 0, 0);
            inner.Controls.Add(_lblProgress, 0, 1);
            inner.Controls.Add(_barProgress, 0, 2);
            inner.Controls.Add(_btnAdvance, 0, 3);

            group.Controls.Add(inner);
            table.Controls.Add(group, 1, 0);
            return table;
        }

        private Control BuildTabs()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };

            var tabUrgent = new TabPage("Next to handle (priority queue)");
            tabUrgent.Controls.Add(BuildUrgentTab());
            tabs.TabPages.Add(tabUrgent);

            var tabCategories = new TabPage("By category (tree)");
            tabCategories.Controls.Add(BuildCategoryTab());
            tabs.TabPages.Add(tabCategories);

            var tabAreas = new TabPage("Service areas");
            tabAreas.Controls.Add(BuildAreasTab());
            tabs.TabPages.Add(tabAreas);

            var tabStructures = new TabPage("Data structures");
            tabStructures.Controls.Add(BuildStructuresTab());
            tabs.TabPages.Add(tabStructures);

            return tabs;
        }

        private Control BuildUrgentTab()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(6)
            };
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false
            };

            _lblNext = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(2, 9, 12, 0)
            };
            top.Controls.Add(_lblNext);

            _btnHandleNext = MakeButton("Handle next urgent request (demo)", 250);
            _btnHandleNext.Click += (s, e) => HandleNextUrgent();
            top.Controls.Add(_btnHandleNext);

            top.Controls.Add(new Label
            {
                Text = "Double-click a row to find it in the list above.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(14, 9, 0, 0)
            });

            _listUrgent = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                MultiSelect = false
            };
            _listUrgent.Columns.Add("Rank", 55);
            _listUrgent.Columns.Add("Tracking no.", 100);
            _listUrgent.Columns.Add("Priority", 70);
            _listUrgent.Columns.Add("Category", 220);
            _listUrgent.Columns.Add("Status", 110);
            _listUrgent.Columns.Add("Last updated", 140);
            _listUrgent.Columns.Add("Location", 260);
            _listUrgent.ItemActivate += ListUrgent_ItemActivate;

            table.Controls.Add(top, 0, 0);
            table.Controls.Add(_listUrgent, 0, 1);
            return table;
        }

        private Control BuildCategoryTab()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(6)
            };
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false
            };

            Button expand = MakeButton("Expand all", 100);
            expand.Click += (s, e) => _treeCategories.ExpandAll();
            Button collapse = MakeButton("Collapse all", 100);
            collapse.Click += (s, e) =>
            {
                _treeCategories.CollapseAll();
                if (_treeCategories.Nodes.Count > 0)
                    _treeCategories.Nodes[0].Expand();
            };
            top.Controls.Add(expand);
            top.Controls.Add(collapse);
            top.Controls.Add(new Label
            {
                Text = "Municipality \u2192 Category \u2192 Request. Click a request to see its details.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(14, 9, 0, 0)
            });

            _treeCategories = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false
            };
            _treeCategories.AfterSelect += TreeCategories_AfterSelect;

            table.Controls.Add(top, 0, 0);
            table.Controls.Add(_treeCategories, 0, 1);
            return table;
        }

        private Control BuildAreasTab()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _gridAreas = CreateGrid();
            AddColumn(_gridAreas, "colAreaName", "Service area", 50f, null);
            AddColumn(_gridAreas, "colAreaTotal", "Requests", 25f, null);
            AddColumn(_gridAreas, "colAreaOpen", "Not yet resolved", 25f, null);
            table.Controls.Add(_gridAreas, 0, 0);

            _areaGraphHost = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _areaGraphHost.Controls.Add(new Label
            {
                Text = "Road network and minimum spanning tree: added in Step 8b.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            });
            table.Controls.Add(_areaGraphHost, 1, 0);

            BuildAreaGraphPanel(_areaGraphHost);
            return table;
        }

        private Control BuildStructuresTab()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(6)
            };
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false
            };

            top.Controls.Add(MakeFieldLabel("Structure:"));
            _cmbStructure = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 330,
                Margin = new Padding(2, 4, 2, 2)
            };
            _cmbStructure.Items.Add("AVL tree - tracking-number index");
            _cmbStructure.Items.Add("Red-black tree - date-reported index");
            _cmbStructure.Items.Add("Binary heap - next to handle");
            _cmbStructure.Items.Add("General tree - category view");
            _cmbStructure.SelectedIndex = 0;
            _cmbStructure.SelectedIndexChanged += CmbStructure_SelectedIndexChanged;
            top.Controls.Add(_cmbStructure);

            _btnLookupDemo = MakeButton("Compare lookups: tree vs list scan", 270);
            _btnLookupDemo.Click += (s, e) =>
            {
                _lookupDemoShown = true;
                RefreshStructureText();
            };
            top.Controls.Add(_btnLookupDemo);

            _txtStructure = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.White
            };

            table.Controls.Add(top, 0, 0);
            table.Controls.Add(_txtStructure, 0, 1);
            return table;
        }

        private Control BuildFooter()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _btnBack = MakeButton("Back to Main Menu", 170);
            _btnBack.Click += (s, e) => Close();

            _lblStatusBar = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(12, 9, 0, 0)
            };

            table.Controls.Add(_btnBack, 0, 0);
            table.Controls.Add(_lblStatusBar, 1, 0);
            return table;
        }

        // ---------------------------------------------------------------------
        //  Small control factories
        // ---------------------------------------------------------------------
        private static Label MakeFieldLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = new Padding(10, 8, 2, 0) };
        }

        private static Button MakeButton(string text, int width)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Brand,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 2, 2, 2),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Brand;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Brand;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return grid;
        }

        private static void AddColumn(DataGridView grid, string name, string header, float weight, string format)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            if (format != null)
                column.DefaultCellStyle.Format = format;

            grid.Columns.Add(column);
        }

        // =====================================================================
        //  Refreshing what is on screen
        // =====================================================================
        private void RefreshAll()
        {
            ApplyFilters();
            RefreshUrgentQueue();
            RebuildCategoryTree();
            RefreshAreas();
            RefreshStructureText();
            ShowDetails(_focusedIssue);
        }

        private List<Issue> GetFilteredRequests()
        {
            _lblFilterNote.Text = string.Empty;
            List<Issue> list;

            if (_chkDate.Checked)
            {
                if (_dtFrom.Value.Date > _dtTo.Value.Date)
                {
                    _lblFilterNote.Text = "The 'From' date is after the 'To' date, so no requests match.";
                    return new List<Issue>();
                }

                list = _service.GetByDateRange(_dtFrom.Value, _dtTo.Value);   // red-black tree range query
            }
            else
            {
                list = _service.GetAllRequests();                             // AVL tree in-order walk
            }

            if (_cmbStatus.SelectedIndex > 0)
            {
                var wanted = (RequestStatus)(_cmbStatus.SelectedIndex - 1);
                list = list.Where(i => i.Status == wanted).ToList();
            }

            return list;
        }

        private void ApplyFilters()
        {
            List<Issue> list = GetFilteredRequests();

            _suspendEvents = true;
            try
            {
                PopulateGrid(list);
            }
            finally
            {
                _suspendEvents = false;
            }

            if (_focusedIssue == null || !TrySelectRow(_focusedIssue))
                _gridRequests.ClearSelection();

            UpdateSummary(list.Count);
        }

        private void PopulateGrid(List<Issue> list)
        {
            _gridRequests.Rows.Clear();

            foreach (Issue issue in list)
            {
                int index = _gridRequests.Rows.Add(
                    issue.TrackingNumber,
                    issue.Category,
                    issue.Location,
                    issue.Area,
                    issue.Priority,
                    issue.StatusText,
                    issue.DateReported,
                    issue.LastUpdated);

                DataGridViewRow row = _gridRequests.Rows[index];
                row.Tag = issue;

                DataGridViewCell statusCell = row.Cells["colStatus"];
                statusCell.Style.BackColor = StatusColour(issue.Status);
                statusCell.Style.ForeColor = Color.Black;
            }
        }

        private static Color StatusColour(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Submitted: return Color.FromArgb(214, 230, 245);
                case RequestStatus.UnderReview: return Color.FromArgb(255, 240, 200);
                case RequestStatus.InProgress: return Color.FromArgb(255, 221, 190);
                default: return Color.FromArgb(205, 235, 213);
            }
        }

        private void UpdateSummary(int shown)
        {
            if (_service.Count == 0)
            {
                _lblSummary.Text = "No service requests have been submitted yet.";
                return;
            }

            _lblSummary.Text = string.Format(CultureInfo.InvariantCulture,
                "Showing {0} of {1} requests   |   {2} not yet resolved   |   {3} resolved",
                shown, _service.Count, _service.OpenCount, _service.Count - _service.OpenCount);
        }

        private void RefreshUrgentQueue()
        {
            _listUrgent.BeginUpdate();
            try
            {
                _listUrgent.Items.Clear();

                int rank = 1;
                foreach (Issue issue in _service.GetUrgentQueue(10))   // heap sort on a copy
                {
                    var item = new ListViewItem(new[]
                    {
                        rank.ToString(CultureInfo.InvariantCulture),
                        issue.TrackingNumber,
                        issue.Priority.ToString(CultureInfo.InvariantCulture),
                        issue.Category,
                        issue.StatusText,
                        issue.LastUpdated.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        issue.Location
                    });
                    item.Tag = issue;
                    _listUrgent.Items.Add(item);
                    rank++;
                }
            }
            finally
            {
                _listUrgent.EndUpdate();
            }

            Issue next = _service.NextToHandle;   // heap Peek: O(1)
            if (next == null)
            {
                _lblNext.Text = "No open requests.";
                _btnHandleNext.Enabled = false;
            }
            else
            {
                _lblNext.Text = string.Format(CultureInfo.InvariantCulture,
                    "Next to handle: {0} ({1}, priority {2})", next.TrackingNumber, next.Category, next.Priority);
                _btnHandleNext.Enabled = true;
            }
        }

        private void RebuildCategoryTree()
        {
            var expanded = new HashSet<string>();
            if (_treeCategories.Nodes.Count > 0)
            {
                foreach (TreeNode category in _treeCategories.Nodes[0].Nodes)
                {
                    if (category.IsExpanded)
                        expanded.Add(category.Name);
                }
            }

            _treeCategories.BeginUpdate();
            try
            {
                _treeCategories.Nodes.Clear();
                TreeNode root = ToTreeNode(_service.CategoryTree.Root);
                _treeCategories.Nodes.Add(root);
                root.Expand();

                foreach (TreeNode category in root.Nodes)
                {
                    if (expanded.Contains(category.Name))
                        category.Expand();
                }
            }
            finally
            {
                _treeCategories.EndUpdate();
            }

            _categoryTreeBuilt = true;
        }

        private TreeNode ToTreeNode(GeneralTreeNode<CategoryTreeEntry> node)
        {
            CategoryTreeEntry entry = node.Value;
            string text = entry.ToString();

            // Category nodes show how many requests they hold
            if (!entry.IsRequest && node.Parent != null)
                text += " (" + node.Children.Count.ToString(CultureInfo.InvariantCulture) + ")";

            var treeNode = new TreeNode(text) { Name = entry.Label, Tag = entry.Issue };

            // Requests in tracking-number order; categories keep their order
            foreach (GeneralTreeNode<CategoryTreeEntry> child in
                     node.Children.OrderBy(c => c.Value.IsRequest ? c.Value.Issue.Sequence : 0))
            {
                treeNode.Nodes.Add(ToTreeNode(child));
            }

            return treeNode;
        }

        private void RefreshAreas()
        {
            _gridAreas.Rows.Clear();
            foreach (AreaSummary summary in _service.GetAreaSummaries())
            {
                int index = _gridAreas.Rows.Add(summary.Area, summary.Total, summary.Open);
                if (summary.Total == 0)
                    _gridAreas.Rows[index].DefaultCellStyle.ForeColor = Color.Gray;
            }
            _gridAreas.ClearSelection();

            RefreshAreaGraphPanel();
        }

        // =====================================================================
        //  Selected request (details panel)
        // =====================================================================
        private void FocusIssue(Issue issue)
        {
            _focusedIssue = issue;
            ShowDetails(issue);
        }

        private void ShowDetails(Issue issue)
        {
            if (issue == null)
            {
                _txtDetails.Text = "Select a request in the list to see its details and progress.";
                _lblProgress.Text = "Progress";
                _barProgress.Value = 0;
                _btnAdvance.Enabled = false;
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(issue.TrackingNumber);
            sb.AppendLine(issue.Category + "  |  Priority " + issue.Priority);
            sb.AppendLine("Location: " + issue.Location);
            sb.AppendLine("Service area: " + issue.Area);
            sb.AppendLine("Reported: " + issue.DateReported.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            sb.AppendLine();
            sb.AppendLine("Description:");
            sb.AppendLine(issue.Description);
            sb.AppendLine();
            sb.AppendLine("Attachments: " + issue.AttachmentPaths.Count);
            sb.AppendLine();
            sb.AppendLine("Status history:");
            foreach (StatusChange change in issue.StatusHistory)
            {
                sb.AppendLine("  " + Issue.GetStatusText(change.Status) + "  (" +
                              change.Timestamp.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ")");
            }

            if (issue.Status != RequestStatus.Resolved)
                sb.AppendLine("  Next: " + Issue.GetStatusText((RequestStatus)((int)issue.Status + 1)));

            _txtDetails.Text = sb.ToString();
            _txtDetails.Select(0, 0);

            int stage = (int)issue.Status + 1;
            _lblProgress.Text = string.Format(CultureInfo.InvariantCulture,
                "Progress: {0} (stage {1} of 4)", issue.StatusText, stage);
            _barProgress.Value = stage * 25;
            _btnAdvance.Enabled = issue.Status != RequestStatus.Resolved;
        }

        // =====================================================================
        //  User actions
        // =====================================================================
        private void TxtTracking_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FindRequest();
            }
        }

        private void FindRequest()
        {
            string text = _txtTracking.Text.Trim();
            if (text.Length == 0)
            {
                SetFindResult("Please enter a tracking number, for example SR-000012 or just 12.", false);
                return;
            }

            int sequence;
            if (!Issue.TryParseTrackingNumber(text, out sequence))
            {
                SetFindResult("'" + text + "' is not a valid tracking number. Use a format like SR-000012, or just 12.", false);
                return;
            }

            string formatted = "SR-" + sequence.ToString("D6", CultureInfo.InvariantCulture);
            Issue issue = _service.FindByTrackingNumber(text);       // AVL tree lookup
            int comparisons = _service.LastLookupComparisons;

            if (issue == null)
            {
                SetFindResult(string.Format(CultureInfo.InvariantCulture,
                    "No request found with tracking number {0} (the AVL tree answered in {1} comparisons).",
                    formatted, comparisons), false);
                return;
            }

            SetFindResult(string.Format(CultureInfo.InvariantCulture,
                "Found {0}: {1}. The AVL tree needed {2} comparison(s); scanning the list could need up to {3}.",
                issue.TrackingNumber, issue.StatusText, comparisons, _service.Count), true);

            SelectInGrid(issue);
        }

        private void SetFindResult(string message, bool success)
        {
            _lblFindResult.ForeColor = success ? GoodColour : BadColour;
            _lblFindResult.Text = message;
        }

        private void SetStatus(string message)
        {
            _lblStatusBar.Text = message;
        }

        private void ChkDate_CheckedChanged(object sender, EventArgs e)
        {
            if (_suspendEvents)
                return;

            _dtFrom.Enabled = _chkDate.Checked;
            _dtTo.Enabled = _chkDate.Checked;

            if (_chkDate.Checked && !_dateRangeInitialised)
                InitialiseDateRange();

            ApplyFilters();
        }

        /// <summary>The first time the date filter is switched on, span the pickers over the stored requests.</summary>
        private void InitialiseDateRange()
        {
            DateSequenceKey minKey;
            DateSequenceKey maxKey;
            Issue ignoredA;
            Issue ignoredB;

            if (_service.DateIndex.TryGetMin(out minKey, out ignoredA) &&
                _service.DateIndex.TryGetMax(out maxKey, out ignoredB))
            {
                _suspendEvents = true;
                try
                {
                    _dtFrom.Value = minKey.Date.Date;
                    _dtTo.Value = maxKey.Date.Date;
                }
                finally
                {
                    _suspendEvents = false;
                }
                _dateRangeInitialised = true;
            }
        }

        private void DateFilter_Changed(object sender, EventArgs e)
        {
            if (_suspendEvents)
                return;

            ApplyFilters();
        }

        private void ClearFilters()
        {
            _suspendEvents = true;
            try
            {
                _chkDate.Checked = false;
                _dtFrom.Enabled = false;
                _dtTo.Enabled = false;
                _cmbStatus.SelectedIndex = 0;
            }
            finally
            {
                _suspendEvents = false;
            }

            ApplyFilters();
        }

        private void GridRequests_SelectionChanged(object sender, EventArgs e)
        {
            if (_suspendEvents)
                return;

            if (_gridRequests.SelectedRows.Count > 0)
            {
                Issue issue = _gridRequests.SelectedRows[0].Tag as Issue;
                if (issue != null)
                    FocusIssue(issue);
            }
        }

        private void ListUrgent_ItemActivate(object sender, EventArgs e)
        {
            if (_listUrgent.SelectedItems.Count == 0)
                return;

            Issue issue = _listUrgent.SelectedItems[0].Tag as Issue;
            if (issue != null)
                SelectInGrid(issue);
        }

        private void TreeCategories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Issue issue = e.Node.Tag as Issue;
            if (issue != null)
                FocusIssue(issue);
        }

        /// <summary>Selects the request in the grid, clearing the filters first if they are hiding it.</summary>
        private void SelectInGrid(Issue issue)
        {
            if (!TrySelectRow(issue))
            {
                ClearFilters();
                TrySelectRow(issue);
            }

            FocusIssue(issue);
        }

        private bool TrySelectRow(Issue issue)
        {
            foreach (DataGridViewRow row in _gridRequests.Rows)
            {
                if (ReferenceEquals(row.Tag, issue))
                {
                    _gridRequests.CurrentCell = row.Cells[0];
                    row.Selected = true;
                    return true;
                }
            }
            return false;
        }

        private void AdvanceFocusedIssue()
        {
            Issue issue = _focusedIssue;
            if (issue == null)
            {
                MessageBox.Show("Please select a request first.", "Advance status",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string before = issue.StatusText;
            if (!_repository.AdvanceStatus(issue))   // raises IssueUpdated, which refreshes everything
            {
                MessageBox.Show(issue.TrackingNumber + " is already resolved.", "Advance status",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetStatus(issue.TrackingNumber + " moved from " + before + " to " + issue.StatusText + ".");
        }

        private void HandleNextUrgent()
        {
            Issue next = _service.NextToHandle;
            if (next == null)
            {
                MessageBox.Show("There are no open requests to handle.", "Next to handle",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string before = next.StatusText;
            _repository.AdvanceStatus(next);
            SetStatus("Handled the most urgent request: " + next.TrackingNumber + " moved from " +
                      before + " to " + next.StatusText + ". The priority queue has been rebuilt.");
        }

        // =====================================================================
        //  Data structures tab
        // =====================================================================
        private void CmbStructure_SelectedIndexChanged(object sender, EventArgs e)
        {
            _lookupDemoShown = false;
            RefreshStructureText();
        }

        private void RefreshStructureText()
        {
            if (_lookupDemoShown)
            {
                _txtStructure.Text = BuildLookupComparisonText();
                return;
            }

            var sb = new StringBuilder();

            switch (_cmbStructure.SelectedIndex)
            {
                case 1:
                    AppendRedBlackText(sb);
                    break;
                case 2:
                    AppendHeapText(sb);
                    break;
                case 3:
                    AppendCategoryTreeText(sb);
                    break;
                default:
                    AppendAvlText(sb);
                    break;
            }

            _txtStructure.Text = sb.ToString();
            _txtStructure.Select(0, 0);
        }

        private void AppendAvlText(StringBuilder sb)
        {
            AvlTree<int, Issue> avl = _service.SequenceIndex;

            sb.AppendLine("AVL TREE - tracking-number index");
            sb.AppendLine("Key = sequence number (key 12 means SR-000012). Used by the tracking-number search box.");
            sb.AppendLine();
            sb.AppendLine("Requests stored  : " + avl.Count);
            sb.AppendLine("Tree height      : " + avl.Height + "   (any lookup needs at most this many comparisons)");
            sb.AppendLine("Rotations made   : " + avl.RotationCount);
            sb.AppendLine("Balanced & valid : " + (avl.Validate() ? "yes" : "NO"));
            sb.AppendLine();
            sb.AppendLine("Diagram (root on the left, larger keys on top, h = node height):");
            foreach (string line in avl.ToDiagramLines())
                sb.AppendLine(line);
        }

        private void AppendRedBlackText(StringBuilder sb)
        {
            RedBlackTree<DateSequenceKey, Issue> rb = _service.DateIndex;

            sb.AppendLine("RED-BLACK TREE - date-reported index");
            sb.AppendLine("Key = date reported + sequence number. Used by the date-range filter.");
            sb.AppendLine();
            sb.AppendLine("Requests stored  : " + rb.Count);
            sb.AppendLine("Tree height      : " + rb.Height);
            sb.AppendLine("Black height     : " + rb.BlackHeight);
            sb.AppendLine("Rotations made   : " + rb.RotationCount);
            sb.AppendLine("Recolourings     : " + rb.RecolourCount);
            sb.AppendLine("All rules valid  : " + (rb.Validate() ? "yes" : "NO"));
            sb.AppendLine();
            sb.AppendLine("Diagram (root on the left, later dates on top, (R) = red, (B) = black):");
            foreach (string line in rb.ToDiagramLines())
                sb.AppendLine(line);
        }

        private void AppendHeapText(StringBuilder sb)
        {
            BinaryHeap<Issue> heap = _service.UrgencyHeap;

            sb.AppendLine("BINARY HEAP - next to handle");
            sb.AppendLine("Holds the requests that are not yet resolved. Most urgent on top (priority, then oldest update).");
            sb.AppendLine();
            sb.AppendLine("Open requests    : " + heap.Count);
            sb.AppendLine("Heap height      : " + heap.Height);
            sb.AppendLine("Heap rule valid  : " + (heap.Validate() ? "yes" : "NO"));
            sb.AppendLine("Rebuilds so far  : " + _service.HeapRebuildCount + "   (BuildHeap runs after every status change, O(n))");
            sb.AppendLine("Comparisons made : " + heap.TotalComparisons);
            sb.AppendLine("Swaps made       : " + heap.SwapCount);
            sb.AppendLine();
            sb.AppendLine("Heap by level (siblings are not sorted; P = priority):");
            foreach (string line in heap.ToLevelLines(i => i.TrackingNumber + " (P" + i.Priority + ")"))
                sb.AppendLine(line);
        }

        private void AppendCategoryTreeText(StringBuilder sb)
        {
            GeneralTree<CategoryTreeEntry> tree = _service.CategoryTree;

            sb.AppendLine("GENERAL TREE - category view");
            sb.AppendLine("Municipality -> Category -> Request. Any node can have any number of children.");
            sb.AppendLine();
            sb.AppendLine("Nodes            : " + tree.Count);
            sb.AppendLine("Levels           : " + tree.Height);
            sb.AppendLine();
            sb.AppendLine("Depth-first (pre-order) walk:");
            foreach (GeneralTreeNode<CategoryTreeEntry> node in tree.PreOrder())
                sb.AppendLine(new string(' ', node.Depth * 3) + node.Value);
        }

        private string BuildLookupComparisonText()
        {
            var sb = new StringBuilder();
            List<Issue> all = _service.GetAllRequests();

            sb.AppendLine("FINDING A REQUEST BY TRACKING NUMBER: AVL TREE vs SCANNING THE LIST");
            sb.AppendLine();

            if (all.Count == 0)
            {
                sb.AppendLine("There are no requests yet.");
                return sb.ToString();
            }

            sb.AppendLine("Requests (n) : " + all.Count);
            sb.AppendLine("AVL height   : " + _service.SequenceIndex.Height);
            sb.AppendLine();
            sb.AppendLine("Request to find".PadRight(24) + "AVL tree".PadRight(14) + "List scan");
            sb.AppendLine(new string('-', 48));

            var picks = new List<int> { 0, all.Count / 2, all.Count - 1 };
            foreach (int position in picks.Distinct())
            {
                Issue target = all[position];
                _service.FindByTrackingNumber(target.TrackingNumber);
                int treeComparisons = _service.LastLookupComparisons;
                int scanComparisons = LinearComparisons(target.Sequence);

                sb.AppendLine(target.TrackingNumber.PadRight(24) +
                              treeComparisons.ToString(CultureInfo.InvariantCulture).PadRight(14) +
                              scanComparisons.ToString(CultureInfo.InvariantCulture));
            }

            _service.FindByTrackingNumber("SR-999999");
            sb.AppendLine("SR-999999 (missing)".PadRight(24) +
                          _service.LastLookupComparisons.ToString(CultureInfo.InvariantCulture).PadRight(14) +
                          LinearComparisons(999999).ToString(CultureInfo.InvariantCulture));

            sb.AppendLine();
            sb.AppendLine("The list scan grows with n; the AVL tree grows only with log n.");
            return sb.ToString();
        }

        /// <summary>Comparisons a plain scan of the repository list would make to find this sequence number.</summary>
        private int LinearComparisons(int sequence)
        {
            int count = 0;
            foreach (Issue issue in _repository.GetAllIssues())
            {
                count++;
                if (issue.Sequence == sequence)
                    return count;
            }
            return count;
        }
    }
}