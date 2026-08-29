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
            // Only "Report Issues" is functional in Part 1.
            // These are intentionally disabled per the brief, to be enabled in Part 2/3.
            btnLocalEvents.Enabled = false;
            btnServiceStatus.Enabled = false;

            // Optional: tooltip explaining why they're disabled, for user clarity.
            var toolTip = new ToolTip();
            toolTip.SetToolTip(btnLocalEvents, "Coming soon");
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
    }
}