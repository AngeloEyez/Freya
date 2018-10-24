namespace Freya
{
    partial class FormSetting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSetting));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_WebService = new System.Windows.Forms.TextBox();
            this.textBox_SMTPServer = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_Email = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_LogLevel = new System.Windows.Forms.ComboBox();
            this.Options_Cancel = new System.Windows.Forms.Button();
            this.Options_OK = new System.Windows.Forms.Button();
            this.textBox_FeatureByte = new System.Windows.Forms.TextBox();
            this.comboBox_SMTPLogLevel = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(13, 13);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(388, 234);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.comboBox_SMTPLogLevel);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.textBox_WebService);
            this.tabPage1.Controls.Add(this.textBox_SMTPServer);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.textBox_Email);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(380, 208);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Mail";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(85, 69);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(274, 28);
            this.label5.TabIndex = 3;
            this.label5.Text = "請打開  SuperNote\\XML\\SystemSet\\ServerAddress.xml  文件尋找並填入以下資料";
            // 
            // textBox_WebService
            // 
            this.textBox_WebService.Location = new System.Drawing.Point(87, 128);
            this.textBox_WebService.Name = "textBox_WebService";
            this.textBox_WebService.Size = new System.Drawing.Size(272, 22);
            this.textBox_WebService.TabIndex = 2;
            this.textBox_WebService.TextChanged += new System.EventHandler(this.textBox_WebService_TextChanged);
            // 
            // textBox_SMTPServer
            // 
            this.textBox_SMTPServer.Location = new System.Drawing.Point(87, 100);
            this.textBox_SMTPServer.Name = "textBox_SMTPServer";
            this.textBox_SMTPServer.Size = new System.Drawing.Size(272, 22);
            this.textBox_SMTPServer.TabIndex = 2;
            this.textBox_SMTPServer.TextChanged += new System.EventHandler(this.textBox_SMTPServer_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 131);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "WebService";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 103);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 12);
            this.label3.TabIndex = 1;
            this.label3.Text = "SmtpServerIp";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "E-Mail Address";
            // 
            // textBox_Email
            // 
            this.textBox_Email.Location = new System.Drawing.Point(87, 6);
            this.textBox_Email.Name = "textBox_Email";
            this.textBox_Email.Size = new System.Drawing.Size(272, 22);
            this.textBox_Email.TabIndex = 0;
            this.textBox_Email.TextChanged += new System.EventHandler(this.textBox_Email_TextChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(380, 208);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "DMS";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label1);
            this.tabPage3.Controls.Add(this.comboBox_LogLevel);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(380, 208);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Advanced";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "LogLevel";
            // 
            // comboBox_LogLevel
            // 
            this.comboBox_LogLevel.FormattingEnabled = true;
            this.comboBox_LogLevel.Location = new System.Drawing.Point(62, 11);
            this.comboBox_LogLevel.Name = "comboBox_LogLevel";
            this.comboBox_LogLevel.Size = new System.Drawing.Size(97, 20);
            this.comboBox_LogLevel.TabIndex = 0;
            this.comboBox_LogLevel.ParentChanged += new System.EventHandler(this.comboBox_LogLevel_ParentChanged);
            // 
            // Options_Cancel
            // 
            this.Options_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Options_Cancel.Location = new System.Drawing.Point(245, 254);
            this.Options_Cancel.Name = "Options_Cancel";
            this.Options_Cancel.Size = new System.Drawing.Size(75, 23);
            this.Options_Cancel.TabIndex = 1;
            this.Options_Cancel.Text = "Cancel";
            this.Options_Cancel.UseVisualStyleBackColor = true;
            // 
            // Options_OK
            // 
            this.Options_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Options_OK.Location = new System.Drawing.Point(326, 254);
            this.Options_OK.Name = "Options_OK";
            this.Options_OK.Size = new System.Drawing.Size(75, 23);
            this.Options_OK.TabIndex = 2;
            this.Options_OK.Text = "OK";
            this.Options_OK.UseVisualStyleBackColor = true;
            this.Options_OK.Click += new System.EventHandler(this.Options_OK_Click);
            // 
            // textBox_FeatureByte
            // 
            this.textBox_FeatureByte.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBox_FeatureByte.BackColor = System.Drawing.SystemColors.Control;
            this.textBox_FeatureByte.ForeColor = System.Drawing.SystemColors.GrayText;
            this.textBox_FeatureByte.Location = new System.Drawing.Point(17, 254);
            this.textBox_FeatureByte.Name = "textBox_FeatureByte";
            this.textBox_FeatureByte.ShortcutsEnabled = false;
            this.textBox_FeatureByte.Size = new System.Drawing.Size(100, 22);
            this.textBox_FeatureByte.TabIndex = 3;
            this.textBox_FeatureByte.TabStop = false;
            this.textBox_FeatureByte.TextChanged += new System.EventHandler(this.textBox_FeatureByte_TextChanged);
            // 
            // comboBox_SMTPLogLevel
            // 
            this.comboBox_SMTPLogLevel.FormattingEnabled = true;
            this.comboBox_SMTPLogLevel.Location = new System.Drawing.Point(87, 156);
            this.comboBox_SMTPLogLevel.Name = "comboBox_SMTPLogLevel";
            this.comboBox_SMTPLogLevel.Size = new System.Drawing.Size(121, 20);
            this.comboBox_SMTPLogLevel.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 159);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 12);
            this.label6.TabIndex = 5;
            this.label6.Text = "Log Level";
            // 
            // FormSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(416, 289);
            this.Controls.Add(this.textBox_FeatureByte);
            this.Controls.Add(this.Options_OK);
            this.Controls.Add(this.Options_Cancel);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSetting";
            this.ShowInTaskbar = false;
            this.Text = "Freya Options";
            this.Load += new System.EventHandler(this.FormSetting_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button Options_Cancel;
        private System.Windows.Forms.Button Options_OK;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_LogLevel;
        private System.Windows.Forms.TextBox textBox_FeatureByte;
        private System.Windows.Forms.TextBox textBox_SMTPServer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Email;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_WebService;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBox_SMTPLogLevel;
    }
}