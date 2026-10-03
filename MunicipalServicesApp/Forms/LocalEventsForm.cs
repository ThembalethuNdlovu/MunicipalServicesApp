using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Services;

namespace MunicipalServicesApp.Forms
{
    public partial class LocalEventsForm : Form
    {
        // ---------- Palette (change these to match your other forms) ----------
        private static readonly Color PrimaryColor = Color.FromArgb(0, 90, 156);
        private static readonly Color SecondaryColor = Color.FromArgb(96, 108, 118);
        private static readonly Color AccentColor = Color.FromArgb(0, 128, 128);
        private static readonly Color BackgroundColor = Color.FromArgb(245, 247, 250);
        private static readonly Color AnnouncementBack = Color.FromArgb(255, 243, 205);
        private static readonly Color AnnouncementText = Color.FromArgb(102, 77, 3);

        // ---------- Fonts ----------
        private static readonly Font HeaderFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        private static readonly Font DetailTitleFont = new Font("Segoe UI", 14F, FontStyle.Bold);
        private static readonly Font DetailBoldFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        private static readonly Font DetailNormalFont = new Font("Segoe UI", 10F, FontStyle.Regular);

        // ---------- Controls ----------
        private TextBox txtKeyword;
        private ComboBox cmbCategory;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Button btnSearch;
        private Button btnClear;
        private Button btnFeatured;
        private Button btnBack;
        private ListView lvEvents;
        private ListView lvRecommendations;
        private RichTextBox rtbDetails;
        private Label lblAnnouncement;
        private Label lblResults;
        private Label lblRecent;
        private System.Windows.Forms.Timer _announcementTimer;

        public LocalEventsForm()
        {
            InitializeComponent();
            BuildUi();
            LoadInitialData();
        }

        // =====================================================================
        //  UI construction
        // =====================================================================
        private void BuildUi()
        {
            Text = "Local Events and Announcements";
            BackColor = BackgroundColor;
            Font = new Font("Segoe UI", 9.5F);
            MinimumSize = new Size(980, 640);
            StartPosition = FormStartPosition.CenterScreen;

            // Root layout: header / announcement bar / body / footer
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = BackgroundColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildAnnouncementBar(), 0, 1);
            root.Controls.Add(BuildBody(), 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);

            Controls.Add(root);

            AcceptButton = btnSearch;

            // Rotate announcements every 5 seconds
            _announcementTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _announcementTimer.Tick += (s, e) => ShowNextAnnouncement();
            _announcementTimer.Start();
        }

        private Control BuildHeader()
        {
            var title = new Label
            {
                Text = "Local Events & Announcements",
                Dock = DockStyle.Fill,
                Font = HeaderFont,
                ForeColor = Color.White,
                BackColor = PrimaryColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Margin = Padding.Empty
            };
            return title;
        }

        private Control BuildAnnouncementBar()
        {
            lblAnnouncement = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = AnnouncementBack,
                ForeColor = AnnouncementText,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 16, 0),
                AutoEllipsis = true,
                Margin = Padding.Empty
            };
            return lblAnnouncement;
        }

        private Control BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12),
                BackColor = BackgroundColor,
                Margin = Padding.Empty
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            body.Controls.Add(BuildLeftPanel(), 0, 0);
            body.Controls.Add(BuildRightPanel(), 1, 0);
            return body;
        }

        private Control BuildLeftPanel()
        {
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0, 0, 6, 0)
            };
            left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            left.Controls.Add(BuildSearchGroup(), 0, 0);
            left.Controls.Add(BuildResultsHeader(), 0, 1);
            left.Controls.Add(BuildEventsList(), 0, 2);

            lblRecent = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SecondaryColor,
                AutoEllipsis = true,
                Margin = Padding.Empty
            };
            left.Controls.Add(lblRecent, 0, 3);

            return left;
        }

        private Control BuildSearchGroup()
        {
            var group = new GroupBox
            {
                Text = "Find events",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Margin = new Padding(0)
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(4, 0, 4, 0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            table.Controls.Add(MakeLabel("Keyword"), 0, 0);
            table.Controls.Add(MakeLabel("Category"), 1, 0);
            table.Controls.Add(MakeLabel("From (optional)"), 2, 0);
            table.Controls.Add(MakeLabel("To (optional)"), 3, 0);

            var regular = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            txtKeyword = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 3, 8, 3),
                Font = regular
            };

            cmbCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 3, 8, 3),
                Font = regular
            };

            dtpFrom = new DateTimePicker
            {
                ShowCheckBox = true,
                Checked = false,
                Format = DateTimePickerFormat.Short,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 3, 8, 3),
                Font = regular
            };

            dtpTo = new DateTimePicker
            {
                ShowCheckBox = true,
                Checked = false,
                Format = DateTimePickerFormat.Short,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 3, 8, 3),
                Font = regular
            };

            btnSearch = new Button
            {
                Text = "Search",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 30,
                Margin = new Padding(3, 3, 3, 3)
            };
            StyleButton(btnSearch, PrimaryColor);
            btnSearch.Click += (s, e) => PerformSearch();

            table.Controls.Add(txtKeyword, 0, 1);
            table.Controls.Add(cmbCategory, 1, 1);
            table.Controls.Add(dtpFrom, 2, 1);
            table.Controls.Add(dtpTo, 3, 1);
            table.Controls.Add(btnSearch, 4, 1);

            group.Controls.Add(table);
            return group;
        }

        private Control BuildResultsHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            lblResults = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Margin = Padding.Empty
            };

            btnClear = new Button { Text = "Clear", Dock = DockStyle.Fill, Margin = new Padding(3, 4, 3, 4) };
            StyleButton(btnClear, SecondaryColor);
            btnClear.Click += (s, e) => ClearFilters();

            btnFeatured = new Button { Text = "Featured", Dock = DockStyle.Fill, Margin = new Padding(3, 4, 0, 4) };
            StyleButton(btnFeatured, AccentColor);
            btnFeatured.Click += (s, e) => ShowFeatured();

            header.Controls.Add(lblResults, 0, 0);
            header.Controls.Add(btnClear, 1, 0);
            header.Controls.Add(btnFeatured, 2, 0);
            return header;
        }

        private Control BuildEventsList()
        {
            lvEvents = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                MultiSelect = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            lvEvents.Columns.Add("Date", 125);
            lvEvents.Columns.Add("Event", 240);
            lvEvents.Columns.Add("Category", 90);
            lvEvents.Columns.Add("Location", 130);

            lvEvents.SelectedIndexChanged += OnEventSelected;
            lvEvents.SizeChanged += (s, e) => ResizeEventColumns();
            return lvEvents;
        }

        private Control BuildRightPanel()
        {
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(6, 0, 0, 0)
            };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

            // Details
            var detailsGroup = new GroupBox
            {
                Text = "Event details",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 6)
            };
            rtbDetails = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                TabStop = false
            };
            detailsGroup.Controls.Add(rtbDetails);

            // Recommendations
            var recGroup = new GroupBox
            {
                Text = "Recommended for you",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(8),
                Margin = new Padding(0, 6, 0, 0)
            };
            lvRecommendations = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            lvRecommendations.Columns.Add("Event", 150);
            lvRecommendations.Columns.Add("Why", 150);
            lvRecommendations.SelectedIndexChanged += OnRecommendationSelected;
            lvRecommendations.SizeChanged += (s, e) => ResizeRecommendationColumns();
            recGroup.Controls.Add(lvRecommendations);

            right.Controls.Add(detailsGroup, 0, 0);
            right.Controls.Add(recGroup, 0, 1);
            return right;
        }

        private Control BuildFooter()
        {
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12, 9, 12, 9),
                Margin = Padding.Empty
            };

            btnBack = new Button
            {
                Text = "Back to Main Menu",
                Dock = DockStyle.Left,
                Width = 200
            };
            StyleButton(btnBack, SecondaryColor);
            btnBack.Click += (s, e) => Close();

            footer.Controls.Add(btnBack);
            return footer;
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Margin = new Padding(3, 0, 3, 0)
            };
        }

        private static void StyleButton(Button button, Color backColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        // =====================================================================
        //  Data loading and searching
        // =====================================================================
        private void LoadInitialData()
        {
            cmbCategory.Items.Add("All Categories");
            foreach (string category in EventRepository.Instance.GetCategories())
                cmbCategory.Items.Add(category);
            cmbCategory.SelectedIndex = 0;

            dtpFrom.Value = DateTime.Today;
            dtpTo.Value = DateTime.Today.AddMonths(1);

            ShowAllUpcoming();
            RefreshRecommendations();
            UpdateRecentSearches();
            ShowNextAnnouncement();

            ResizeEventColumns();
            ResizeRecommendationColumns();
        }

        private void ShowAllUpcoming()
        {
            var events = EventRepository.Instance.Search(null, null, DateTime.Today, null);
            DisplayEvents(events, "Upcoming events");
            ShowPlaceholder();
        }

        private void PerformSearch()
        {
            string keyword = txtKeyword.Text.Trim();
            string category = cmbCategory.SelectedIndex > 0 ? cmbCategory.SelectedItem.ToString() : null;
            DateTime? from = dtpFrom.Checked ? (DateTime?)dtpFrom.Value.Date : null;
            DateTime? to = dtpTo.Checked ? (DateTime?)dtpTo.Value.Date : null;

            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                MessageBox.Show("The 'From' date cannot be later than the 'To' date.",
                                "Invalid date range",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Only show upcoming events unless the user picked an earlier start date
            DateTime? effectiveFrom = from ?? DateTime.Today;
            var results = EventRepository.Instance.Search(keyword, category, effectiveFrom, to);

            // Remember what the user looked for (feeds the recommendations)
            SearchHistoryService.Instance.RecordSearch(keyword, category, from, to);

            DisplayEvents(results, "Search results");
            ShowPlaceholder();
            RefreshRecommendations();
            UpdateRecentSearches();
        }

        private void ClearFilters()
        {
            txtKeyword.Clear();
            cmbCategory.SelectedIndex = 0;
            dtpFrom.Checked = false;
            dtpTo.Checked = false;
            ShowAllUpcoming();
        }

        private void ShowFeatured()
        {
            var featured = EventRepository.Instance.GetFeaturedEvents(6);
            DisplayEvents(featured, "Featured (most important first)");
            ShowPlaceholder();
        }

        // =====================================================================
        //  Displaying data
        // =====================================================================
        private void DisplayEvents(List<LocalEvent> events, string heading)
        {
            lvEvents.BeginUpdate();
            lvEvents.Items.Clear();

            foreach (LocalEvent evt in events)
            {
                var item = new ListViewItem(new[]
                {
                    evt.Date.ToString("ddd dd MMM, HH:mm"),
                    (evt.IsAnnouncement ? "[Notice] " : "") + evt.Title,
                    evt.Category,
                    evt.Location
                });
                item.Tag = evt;
                if (evt.IsAnnouncement)
                    item.BackColor = AnnouncementBack;

                lvEvents.Items.Add(item);
            }

            lvEvents.EndUpdate();

            lblResults.Text = events.Count == 0
                ? "No events match your search. Try different filters."
                : string.Format("{0}: {1} found", heading, events.Count);
        }

        private void RefreshRecommendations()
        {
            var recommendations = RecommendationService.GetRecommendations(5);

            lvRecommendations.BeginUpdate();
            lvRecommendations.Items.Clear();
            foreach (EventRecommendation rec in recommendations)
            {
                var item = new ListViewItem(new[] { rec.Event.Title, rec.Reason });
                item.Tag = rec;
                lvRecommendations.Items.Add(item);
            }
            lvRecommendations.EndUpdate();
        }

        private void UpdateRecentSearches()
        {
            var recent = SearchHistoryService.Instance.GetRecentSearches(3);
            lblRecent.Text = recent.Count == 0
                ? "Recent searches: none yet"
                : "Recent searches: " + string.Join("   |   ", recent.Select(r => FormatSearch(r)));
        }

        private static string FormatSearch(SearchRecord record)
        {
            var parts = new List<string>();
            if (record.Keyword != null) parts.Add("\"" + record.Keyword + "\"");
            if (record.Category != null) parts.Add(record.Category);
            return string.Join(" + ", parts);
        }

        private void ShowNextAnnouncement()
        {
            LocalEvent next = EventRepository.Instance.GetNextAnnouncement();
            if (next == null)
            {
                lblAnnouncement.Text = "No announcements at the moment.";
                return;
            }

            lblAnnouncement.Text = string.Format("ANNOUNCEMENT   |   {0}   -   {1:dd MMM}   -   {2}",
                                                 next.Title, next.Date, next.Location);
        }

        // =====================================================================
        //  Details panel
        // =====================================================================
        private void ShowPlaceholder()
        {
            rtbDetails.Clear();
            AppendStyled("Select an event to see its details.", DetailNormalFont, Color.DimGray);
        }

        private void OnEventSelected(object sender, EventArgs e)
        {
            if (lvEvents.SelectedItems.Count == 0) return;
            var evt = (LocalEvent)lvEvents.SelectedItems[0].Tag;
            ShowDetails(evt, null);
        }

        private void OnRecommendationSelected(object sender, EventArgs e)
        {
            if (lvRecommendations.SelectedItems.Count == 0) return;
            var rec = (EventRecommendation)lvRecommendations.SelectedItems[0].Tag;
            ShowDetails(rec.Event, rec.Reason);
        }

        private void ShowDetails(LocalEvent evt, string recommendationReason)
        {
            rtbDetails.Clear();

            AppendStyled(evt.Title + Environment.NewLine, DetailTitleFont, PrimaryColor);
            AppendStyled((evt.IsAnnouncement ? "Announcement" : "Event") + "  |  " + evt.Category
                         + Environment.NewLine + Environment.NewLine, DetailNormalFont, Color.DimGray);

            AppendStyled("When: ", DetailBoldFont, Color.Black);
            AppendStyled(evt.Date.ToString("dddd, dd MMMM yyyy 'at' HH:mm") + Environment.NewLine, DetailNormalFont, Color.Black);

            AppendStyled("Where: ", DetailBoldFont, Color.Black);
            AppendStyled(evt.Location + Environment.NewLine, DetailNormalFont, Color.Black);

            AppendStyled("Priority: ", DetailBoldFont, Color.Black);
            AppendStyled(PriorityText(evt.Priority) + Environment.NewLine + Environment.NewLine, DetailNormalFont, Color.Black);

            AppendStyled(evt.Description + Environment.NewLine, DetailNormalFont, Color.Black);

            if (!string.IsNullOrEmpty(recommendationReason))
            {
                AppendStyled(Environment.NewLine + "Why we suggest this: ", DetailBoldFont, AccentColor);
                AppendStyled(recommendationReason, DetailNormalFont, AccentColor);
            }

            rtbDetails.SelectionStart = 0;
            rtbDetails.ScrollToCaret();
        }

        private void AppendStyled(string text, Font font, Color color)
        {
            rtbDetails.SelectionStart = rtbDetails.TextLength;
            rtbDetails.SelectionLength = 0;
            rtbDetails.SelectionFont = font;
            rtbDetails.SelectionColor = color;
            rtbDetails.AppendText(text);
        }

        private static string PriorityText(int priority)
        {
            switch (priority)
            {
                case 1: return "Urgent";
                case 2: return "High";
                case 3: return "Normal";
                default: return "Low";
            }
        }

        // =====================================================================
        //  Layout helpers and cleanup
        // =====================================================================
        private void ResizeEventColumns()
        {
            if (lvEvents == null || lvEvents.Columns.Count != 4) return;

            int fixedWidth = lvEvents.Columns[0].Width + lvEvents.Columns[2].Width + lvEvents.Columns[3].Width;
            int available = lvEvents.ClientSize.Width - fixedWidth;
            if (available > 120)
                lvEvents.Columns[1].Width = available;
        }

        private void ResizeRecommendationColumns()
        {
            if (lvRecommendations == null || lvRecommendations.Columns.Count != 2) return;

            int available = lvRecommendations.ClientSize.Width - lvRecommendations.Columns[0].Width;
            if (available > 80)
                lvRecommendations.Columns[1].Width = available;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_announcementTimer != null)
            {
                _announcementTimer.Stop();
                _announcementTimer.Dispose();
            }
            base.OnFormClosed(e);
        }
    }
}