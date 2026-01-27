namespace LogExtractorApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnSelectExcel = new System.Windows.Forms.Button();
            this.txtExcelPath = new System.Windows.Forms.TextBox();
            this.btnSelectLogFolder = new System.Windows.Forms.Button();
            this.txtLogFolderPath = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtStatusLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            //
            // btnSelectExcel
            //
            this.btnSelectExcel.Location = new System.Drawing.Point(463, 23);
            this.btnSelectExcel.Name = "btnSelectExcel";
            this.btnSelectExcel.Size = new System.Drawing.Size(75, 23);
            this.btnSelectExcel.TabIndex = 0;
            this.btnSelectExcel.Text = "찾기...";
            this.btnSelectExcel.UseVisualStyleBackColor = true;
            this.btnSelectExcel.Click += new System.EventHandler(this.btnSelectExcel_Click);
            //
            // txtExcelPath
            //
            this.txtExcelPath.Location = new System.Drawing.Point(100, 24);
            this.txtExcelPath.Name = "txtExcelPath";
            this.txtExcelPath.ReadOnly = true;
            this.txtExcelPath.Size = new System.Drawing.Size(357, 23);
            this.txtExcelPath.TabIndex = 1;
            //
            // btnSelectLogFolder
            //
            this.btnSelectLogFolder.Location = new System.Drawing.Point(463, 62);
            this.btnSelectLogFolder.Name = "btnSelectLogFolder";
            this.btnSelectLogFolder.Size = new System.Drawing.Size(75, 23);
            this.btnSelectLogFolder.TabIndex = 2;
            this.btnSelectLogFolder.Text = "폴더 선택";
            this.btnSelectLogFolder.UseVisualStyleBackColor = true;
            this.btnSelectLogFolder.Click += new System.EventHandler(this.btnSelectLogFolder_Click);
            //
            // txtLogFolderPath
            //
            this.txtLogFolderPath.Location = new System.Drawing.Point(100, 63);
            this.txtLogFolderPath.Name = "txtLogFolderPath";
            this.txtLogFolderPath.ReadOnly = true;
            this.txtLogFolderPath.Size = new System.Drawing.Size(357, 23);
            this.txtLogFolderPath.TabIndex = 3;
            //
            // btnStart
            //
            this.btnStart.Location = new System.Drawing.Point(230, 100);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(120, 35);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "로그 추출 시작";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            //
            // progressBar1
            //
            this.progressBar1.Location = new System.Drawing.Point(24, 150);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(514, 23);
            this.progressBar1.TabIndex = 5;
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(24, 180);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(155, 15);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "파일 및 폴더를 선택하세요.";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "엑셀 파일:";
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 66);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 15);
            this.label2.TabIndex = 8;
            this.label2.Text = "로그 폴더:";
            //
            // txtStatusLog
            //
            this.txtStatusLog.BackColor = System.Drawing.Color.White;
            this.txtStatusLog.Location = new System.Drawing.Point(24, 210);
            this.txtStatusLog.Multiline = true;
            this.txtStatusLog.Name = "txtStatusLog";
            this.txtStatusLog.ReadOnly = true;
            this.txtStatusLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatusLog.Size = new System.Drawing.Size(514, 130);
            this.txtStatusLog.TabIndex = 9;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(564, 360);
            this.Controls.Add(this.txtStatusLog);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.txtLogFolderPath);
            this.Controls.Add(this.btnSelectLogFolder);
            this.Controls.Add(this.txtExcelPath);
            this.Controls.Add(this.btnSelectExcel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "로그 추출 자동화 도구 v2.1 (Unified)";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnSelectExcel;
        private System.Windows.Forms.TextBox txtExcelPath;
        private System.Windows.Forms.Button btnSelectLogFolder;
        private System.Windows.Forms.TextBox txtLogFolderPath;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtStatusLog;
    }
}
