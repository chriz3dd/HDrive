namespace HDriveDiscovery
{
    partial class Form1
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
            this.btn_refresh = new System.Windows.Forms.Button();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.btn_FW_Update = new System.Windows.Forms.Button();
            this.btn_upload_webpage = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar_fwUpdate = new System.Windows.Forms.ToolStripProgressBar();
            this.debugConsole = new System.Windows.Forms.TextBox();
            this.btn_clearLog = new System.Windows.Forms.Button();
            this.btn_refreshHDriveData = new System.Windows.Forms.Button();
            this.onlyShow102 = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_refresh
            // 
            this.btn_refresh.Location = new System.Drawing.Point(16, 750);
            this.btn_refresh.Name = "btn_refresh";
            this.btn_refresh.Size = new System.Drawing.Size(180, 43);
            this.btn_refresh.TabIndex = 2;
            this.btn_refresh.Text = "Ping Network";
            this.btn_refresh.UseVisualStyleBackColor = true;
            this.btn_refresh.Click += new System.EventHandler(this.btn_refresh_Click);
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AllowUserToResizeColumns = false;
            this.dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Location = new System.Drawing.Point(16, 46);
            this.dataGridView.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersWidth = 62;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(1502, 696);
            this.dataGridView.TabIndex = 5;
            this.dataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellClick);
            this.dataGridView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.dataGridView_MouseClick);
            // 
            // btn_FW_Update
            // 
            this.btn_FW_Update.BackColor = System.Drawing.Color.IndianRed;
            this.btn_FW_Update.Location = new System.Drawing.Point(1318, 866);
            this.btn_FW_Update.Name = "btn_FW_Update";
            this.btn_FW_Update.Size = new System.Drawing.Size(180, 43);
            this.btn_FW_Update.TabIndex = 6;
            this.btn_FW_Update.Text = "Batch update all FW";
            this.btn_FW_Update.UseVisualStyleBackColor = false;
            this.btn_FW_Update.Click += new System.EventHandler(this.btn_FW_Batch_Update_Click);
            // 
            // btn_upload_webpage
            // 
            this.btn_upload_webpage.BackColor = System.Drawing.Color.IndianRed;
            this.btn_upload_webpage.Location = new System.Drawing.Point(1318, 817);
            this.btn_upload_webpage.Name = "btn_upload_webpage";
            this.btn_upload_webpage.Size = new System.Drawing.Size(180, 43);
            this.btn_upload_webpage.TabIndex = 8;
            this.btn_upload_webpage.Text = "Batch update all GUIs";
            this.btn_upload_webpage.UseVisualStyleBackColor = false;
            this.btn_upload_webpage.Click += new System.EventHandler(this.btn_Webpage_Batch_Upload_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.progressBar_fwUpdate});
            this.statusStrip1.Location = new System.Drawing.Point(0, 1204);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 21, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1531, 32);
            this.statusStrip1.TabIndex = 9;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            this.statusLabel.Margin = new System.Windows.Forms.Padding(0, 4, 250, 3);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(179, 25);
            this.statusLabel.Text = "toolStripStatusLabel1";
            // 
            // progressBar_fwUpdate
            // 
            this.progressBar_fwUpdate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.progressBar_fwUpdate.Name = "progressBar_fwUpdate";
            this.progressBar_fwUpdate.Size = new System.Drawing.Size(150, 24);
            // 
            // debugConsole
            // 
            this.debugConsole.Location = new System.Drawing.Point(16, 817);
            this.debugConsole.Multiline = true;
            this.debugConsole.Name = "debugConsole";
            this.debugConsole.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.debugConsole.Size = new System.Drawing.Size(1278, 299);
            this.debugConsole.TabIndex = 14;
            // 
            // btn_clearLog
            // 
            this.btn_clearLog.Location = new System.Drawing.Point(1318, 1073);
            this.btn_clearLog.Name = "btn_clearLog";
            this.btn_clearLog.Size = new System.Drawing.Size(180, 43);
            this.btn_clearLog.TabIndex = 15;
            this.btn_clearLog.Text = "Clear log";
            this.btn_clearLog.UseVisualStyleBackColor = true;
            this.btn_clearLog.Click += new System.EventHandler(this.btn_clearLog_Click);
            // 
            // btn_refreshHDriveData
            // 
            this.btn_refreshHDriveData.Location = new System.Drawing.Point(202, 750);
            this.btn_refreshHDriveData.Name = "btn_refreshHDriveData";
            this.btn_refreshHDriveData.Size = new System.Drawing.Size(180, 43);
            this.btn_refreshHDriveData.TabIndex = 16;
            this.btn_refreshHDriveData.Text = "Refresh HDrive data";
            this.btn_refreshHDriveData.UseVisualStyleBackColor = true;
            this.btn_refreshHDriveData.Click += new System.EventHandler(this.btn_detectHDrives);
            // 
            // onlyShow102
            // 
            this.onlyShow102.AutoSize = true;
            this.onlyShow102.Location = new System.Drawing.Point(1379, 12);
            this.onlyShow102.Name = "onlyShow102";
            this.onlyShow102.Size = new System.Drawing.Size(139, 24);
            this.onlyShow102.TabIndex = 17;
            this.onlyShow102.Text = "show .102 only";
            this.onlyShow102.UseVisualStyleBackColor = true;
            this.onlyShow102.CheckedChanged += new System.EventHandler(this.onlyShow102_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1531, 1236);
            this.Controls.Add(this.onlyShow102);
            this.Controls.Add(this.btn_refreshHDriveData);
            this.Controls.Add(this.btn_clearLog);
            this.Controls.Add(this.debugConsole);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btn_upload_webpage);
            this.Controls.Add(this.btn_FW_Update);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.btn_refresh);
            this.Name = "Form1";
            this.Text = "HDrive Discovery 0.6.1";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form1_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_refresh;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.Button btn_FW_Update;
        private System.Windows.Forms.Button btn_upload_webpage;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar_fwUpdate;
        private System.Windows.Forms.TextBox debugConsole;
        private System.Windows.Forms.Button btn_clearLog;
        private System.Windows.Forms.Button btn_refreshHDriveData;
        private System.Windows.Forms.CheckBox onlyShow102;
    }
}

