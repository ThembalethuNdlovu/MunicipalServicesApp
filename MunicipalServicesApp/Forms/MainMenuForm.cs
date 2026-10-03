using System;
using System.Windows.Forms;

namespace MunicipalServicesApp.Forms
{
    public partial class MainMenuForm : Form
    {
        public MainMenuForm()
        {
            InitializeComponent();

            // Visual styling — consistent colour scheme across the app.
            this.BackColor = System.Drawing.Color.FromArgb(240, 244, 248);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(30, 60, 100);

            ConfigureButtonStates();
        }

        private void ConfigureButtonStates()
        {
            // Report Issues (Part 1) and Local Events and Announcements (Part 2) are functional.
            btnReportIssues.Enabled = true;
            btnLocalEvents.Enabled = true;

            // Service Request Status is intentionally disabled until Part 3 (POE).
            btnServiceStatus.Enabled = false;

            var toolTip = new ToolTip();
            toolTip.SetToolTip(btnReportIssues, "Report a problem or request a service");
            toolTip.SetToolTip(btnLocalEvents, "View local events and municipal announcements");
            toolTip.SetToolTip(btnServiceStatus, "Coming soon");
        }

        private void MainMenuForm_Load(object sender, EventArgs e)
        {
            // Intentionally empty for now — placeholder for future startup logic.
        }

        private void btnReportIssues_Click(object sender, EventArgs e)
        {
            var reportForm = new ReportIssueForm();
            reportForm.ShowDialog();
        }

        private void btnLocalEvents_Click(object sender, EventArgs e)
        {
            using (var eventsForm = new LocalEventsForm())
            {
                eventsForm.ShowDialog(this);
            }
        }
    }
}