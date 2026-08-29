using System;
using System.IO;
using System.Windows.Forms;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Services;

namespace MunicipalServicesApp.Forms
{
    public partial class ReportIssueForm : Form
    {
        private const int TotalTrackedFields = 3; // Location, Category, Description

        // Parallel list holding full file paths (ListBox only shows file names for readability).
        private readonly System.Collections.Generic.List<string> _attachedFullPaths =
            new System.Collections.Generic.List<string>();

        public ReportIssueForm()
        {
            InitializeComponent();
        }

        private void ReportIssueForm_Load(object sender, EventArgs e)
        {
            PopulateCategories();
            progressEngagement.Minimum = 0;
            progressEngagement.Maximum = TotalTrackedFields;
            progressEngagement.Value = 0;

            lblEngagement.Text = "Let's get started — fill in the details below.";

            // Visual styling — consistent colour scheme across the app.
            this.BackColor = System.Drawing.Color.FromArgb(240, 244, 248);

            btnSubmit.FlatStyle = FlatStyle.Flat;
            btnSubmit.BackColor = System.Drawing.Color.FromArgb(30, 120, 80);
            btnSubmit.ForeColor = System.Drawing.Color.White;
            btnSubmit.Font = new System.Drawing.Font(btnSubmit.Font, System.Drawing.FontStyle.Bold);

            btnAttach.FlatStyle = FlatStyle.Flat;
            btnAttach.BackColor = System.Drawing.Color.FromArgb(230, 230, 230);

            // Hook up live progress tracking as the user types/selects.
            txtLocation.TextChanged += (s, ev) => UpdateEngagementProgress();
            cmbCategory.SelectedIndexChanged += (s, ev) => UpdateEngagementProgress();
            rtbDescription.TextChanged += (s, ev) => UpdateEngagementProgress();
        }

        private void PopulateCategories()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.AddRange(new object[]
            {
                "Sanitation",
                "Roads",
                "Utilities (Water/Electricity)",
                "Waste Management",
                "Public Safety",
                "Parks and Recreation",
                "Other"
            });
        }

        private void UpdateEngagementProgress()
        {
            int completed = 0;
            if (!string.IsNullOrWhiteSpace(txtLocation.Text)) completed++;
            if (cmbCategory.SelectedIndex != -1) completed++;
            if (!string.IsNullOrWhiteSpace(rtbDescription.Text)) completed++;

            progressEngagement.Value = completed;

            switch (completed)
            {
                case 0:
                    lblEngagement.Text = "Let's get started — fill in the details below.";
                    break;
                case 1:
                    lblEngagement.Text = "Good start! Keep going.";
                    break;
                case 2:
                    lblEngagement.Text = "Almost there — just one more field.";
                    break;
                case 3:
                    lblEngagement.Text = "All set! You're ready to submit.";
                    break;
            }
        }

        private void btnAttach_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Images and Documents|*.jpg;*.jpeg;*.png;*.pdf;*.docx;*.txt|All Files|*.*";
                openFileDialog.Multiselect = true;
                openFileDialog.Title = "Attach files related to this issue";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        lstAttachments.Items.Add(Path.GetFileName(fileName));
                    }

                    foreach (string fullPath in openFileDialog.FileNames)
                    {
                        _attachedFullPaths.Add(fullPath);
                    }
                }
            }
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            var issue = new Issue(txtLocation.Text.Trim(), cmbCategory.SelectedItem.ToString(), rtbDescription.Text.Trim());

            foreach (string path in _attachedFullPaths)
            {
                issue.AddAttachment(path);
            }

            IssueRepository.Instance.AddIssue(issue);

            System.Diagnostics.Debug.WriteLine($"Total issues stored: {IssueRepository.Instance.Count}");

            MessageBox.Show(
                "Thank you! Your issue has been reported successfully.",
                "Report Submitted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ClearForm();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                MessageBox.Show("Please enter a location.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLocation.Focus();
                return false;
            }

            if (cmbCategory.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a category.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(rtbDescription.Text))
            {
                MessageBox.Show("Please provide a description of the issue.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                rtbDescription.Focus();
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            txtLocation.Clear();
            cmbCategory.SelectedIndex = -1;
            rtbDescription.Clear();
            lstAttachments.Items.Clear();
            _attachedFullPaths.Clear();
            progressEngagement.Value = 0;
            lblEngagement.Text = "Let's get started — fill in the details below.";
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}