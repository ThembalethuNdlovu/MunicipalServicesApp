namespace MunicipalServicesApp.Forms
{
    partial class ReportIssueForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblLocation = new System.Windows.Forms.Label();
            this.txtLocation = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.rtbDescription = new System.Windows.Forms.RichTextBox();
            this.btnAttach = new System.Windows.Forms.Button();
            this.lstAttachments = new System.Windows.Forms.ListBox();
            this.progressEngagement = new System.Windows.Forms.ProgressBar();
            this.lblEngagement = new System.Windows.Forms.Label();
            this.btnSubmit = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblLocation
            // 
            this.lblLocation.AutoSize = true;
            this.lblLocation.Location = new System.Drawing.Point(29, 29);
            this.lblLocation.Name = "lblLocation";
            this.lblLocation.Size = new System.Drawing.Size(51, 13);
            this.lblLocation.TabIndex = 0;
            this.lblLocation.Text = "Location:";
            // 
            // txtLocation
            // 
            this.txtLocation.Location = new System.Drawing.Point(98, 26);
            this.txtLocation.Name = "txtLocation";
            this.txtLocation.Size = new System.Drawing.Size(216, 20);
            this.txtLocation.TabIndex = 1;
            // 
            // lblCategory
            // 
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(29, 64);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(52, 13);
            this.lblCategory.TabIndex = 2;
            this.lblCategory.Text = "Category:";
            // 
            // cmbCategory
            // 
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.FormattingEnabled = true;
            this.cmbCategory.Location = new System.Drawing.Point(98, 61);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(216, 21);
            this.cmbCategory.TabIndex = 3;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(29, 96);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(63, 13);
            this.lblDescription.TabIndex = 4;
            this.lblDescription.Text = "Description:";
            // 
            // rtbDescription
            // 
            this.rtbDescription.Location = new System.Drawing.Point(98, 96);
            this.rtbDescription.Name = "rtbDescription";
            this.rtbDescription.Size = new System.Drawing.Size(216, 96);
            this.rtbDescription.TabIndex = 5;
            this.rtbDescription.Text = "";
            // 
            // btnAttach
            // 
            this.btnAttach.Location = new System.Drawing.Point(32, 209);
            this.btnAttach.Name = "btnAttach";
            this.btnAttach.Size = new System.Drawing.Size(79, 23);
            this.btnAttach.TabIndex = 6;
            this.btnAttach.Text = "Attach Media";
            this.btnAttach.UseVisualStyleBackColor = true;
            this.btnAttach.Click += new System.EventHandler(this.btnAttach_Click);
            // 
            // lstAttachments
            // 
            this.lstAttachments.FormattingEnabled = true;
            this.lstAttachments.Location = new System.Drawing.Point(32, 238);
            this.lstAttachments.Name = "lstAttachments";
            this.lstAttachments.Size = new System.Drawing.Size(282, 30);
            this.lstAttachments.TabIndex = 7;
            // 
            // progressEngagement
            // 
            this.progressEngagement.Location = new System.Drawing.Point(32, 276);
            this.progressEngagement.Name = "progressEngagement";
            this.progressEngagement.Size = new System.Drawing.Size(282, 23);
            this.progressEngagement.TabIndex = 8;
            // 
            // lblEngagement
            // 
            this.lblEngagement.AutoSize = true;
            this.lblEngagement.Location = new System.Drawing.Point(29, 333);
            this.lblEngagement.Name = "lblEngagement";
            this.lblEngagement.Size = new System.Drawing.Size(0, 13);
            this.lblEngagement.TabIndex = 9;
            // 
            // btnSubmit
            // 
            this.btnSubmit.Location = new System.Drawing.Point(32, 370);
            this.btnSubmit.Name = "btnSubmit";
            this.btnSubmit.Size = new System.Drawing.Size(102, 23);
            this.btnSubmit.TabIndex = 10;
            this.btnSubmit.Text = "Submit";
            this.btnSubmit.UseVisualStyleBackColor = true;
            this.btnSubmit.Click += new System.EventHandler(this.btnSubmit_Click);
            // 
            // btnBack
            // 
            this.btnBack.Location = new System.Drawing.Point(206, 370);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(108, 23);
            this.btnBack.TabIndex = 11;
            this.btnBack.Text = "Back to Main Menu";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // ReportIssueForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnSubmit);
            this.Controls.Add(this.lblEngagement);
            this.Controls.Add(this.progressEngagement);
            this.Controls.Add(this.lstAttachments);
            this.Controls.Add(this.btnAttach);
            this.Controls.Add(this.rtbDescription);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.cmbCategory);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.txtLocation);
            this.Controls.Add(this.lblLocation);
            this.Name = "ReportIssueForm";
            this.Text = "ReportIssueForm";
            this.Load += new System.EventHandler(this.ReportIssueForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.TextBox txtLocation;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.ComboBox cmbCategory;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.RichTextBox rtbDescription;
        private System.Windows.Forms.Button btnAttach;
        private System.Windows.Forms.ListBox lstAttachments;
        private System.Windows.Forms.ProgressBar progressEngagement;
        private System.Windows.Forms.Label lblEngagement;
        private System.Windows.Forms.Button btnSubmit;
        private System.Windows.Forms.Button btnBack;
    }
}