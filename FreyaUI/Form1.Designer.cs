/*
 * OpaqueMail (https://opaquemail.org/).
 * 
 * Licensed according to the MIT License (http://mit-license.org/).
 * 
 * Copyright © Bert Johnson (https://bertjohnson.com/) of Allcloud Inc. (https://allcloud.com/).
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 */

namespace Freya
{
    partial class Freya
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
            if (notifyIcon1 != null)
            {
                notifyIcon1.Visible = false;
                notifyIcon1.Dispose();
                notifyIcon1 = null;
            }

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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Freya));
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip_NotifyIcon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.contextMenuStrip_MinerOperation = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.enableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.alwaysActiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip_ServiceControl = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.startServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label3 = new System.Windows.Forms.Label();
            this.btn_DMS = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.pictureBox_DMS = new System.Windows.Forms.PictureBox();
            this.label_IMAPQuota = new System.Windows.Forms.Label();
            this.pictureBox_Miner = new System.Windows.Forms.PictureBox();
            this.pictureBox_Service = new System.Windows.Forms.PictureBox();
            this.Btn_Options = new System.Windows.Forms.Button();
            this.btn_DecryptFile = new System.Windows.Forms.Button();
            this.contextMenuStrip_NotifyIcon.SuspendLayout();
            this.contextMenuStrip_MinerOperation.SuspendLayout();
            this.contextMenuStrip_ServiceControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_DMS)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Miner)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Service)).BeginInit();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.BackColor = System.Drawing.SystemColors.Window;
            this.listBox1.ForeColor = System.Drawing.SystemColors.GrayText;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.HorizontalScrollbar = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(6, 38);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(602, 316);
            this.listBox1.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.Window;
            this.label1.Font = new System.Drawing.Font("Corbel", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 14);
            this.label1.TabIndex = 17;
            this.label1.Text = "label1";
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip_NotifyIcon;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Freya";
            this.notifyIcon1.BalloonTipClicked += new System.EventHandler(this.notifyIcon1_BalloonTipClicked);
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // contextMenuStrip_NotifyIcon
            // 
            this.contextMenuStrip_NotifyIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.contextMenuStrip_NotifyIcon.Name = "contextMenuStrip1";
            this.contextMenuStrip_NotifyIcon.Size = new System.Drawing.Size(95, 26);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(94, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 250;
            this.toolTip.ReshowDelay = 100;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Corbel", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(167, -2);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 14);
            this.label2.TabIndex = 28;
            this.label2.Text = "label2";
            // 
            // contextMenuStrip_MinerOperation
            // 
            this.contextMenuStrip_MinerOperation.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableToolStripMenuItem,
            this.disableToolStripMenuItem,
            this.toolStripSeparator1,
            this.alwaysActiveToolStripMenuItem});
            this.contextMenuStrip_MinerOperation.Name = "contextMenuStrip_MinerOperation";
            this.contextMenuStrip_MinerOperation.Size = new System.Drawing.Size(150, 76);
            // 
            // enableToolStripMenuItem
            // 
            this.enableToolStripMenuItem.Name = "enableToolStripMenuItem";
            this.enableToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.enableToolStripMenuItem.Text = "Enable";
            this.enableToolStripMenuItem.Click += new System.EventHandler(this.enableToolStripMenuItem_Click);
            // 
            // disableToolStripMenuItem
            // 
            this.disableToolStripMenuItem.Name = "disableToolStripMenuItem";
            this.disableToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.disableToolStripMenuItem.Text = "Disable";
            this.disableToolStripMenuItem.Click += new System.EventHandler(this.disableToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(146, 6);
            // 
            // alwaysActiveToolStripMenuItem
            // 
            this.alwaysActiveToolStripMenuItem.CheckOnClick = true;
            this.alwaysActiveToolStripMenuItem.Name = "alwaysActiveToolStripMenuItem";
            this.alwaysActiveToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.alwaysActiveToolStripMenuItem.Text = "Always Active";
            this.alwaysActiveToolStripMenuItem.Click += new System.EventHandler(this.alwaysActiveToolStripMenuItem_Click);
            // 
            // contextMenuStrip_ServiceControl
            // 
            this.contextMenuStrip_ServiceControl.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startServiceToolStripMenuItem,
            this.stopServiceToolStripMenuItem});
            this.contextMenuStrip_ServiceControl.Name = "contextMenuStrip_ServiceControl";
            this.contextMenuStrip_ServiceControl.Size = new System.Drawing.Size(191, 48);
            // 
            // startServiceToolStripMenuItem
            // 
            this.startServiceToolStripMenuItem.Name = "startServiceToolStripMenuItem";
            this.startServiceToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.startServiceToolStripMenuItem.Text = "SetUIPort";
            this.startServiceToolStripMenuItem.Click += new System.EventHandler(this.startServiceToolStripMenuItem_Click);
            // 
            // stopServiceToolStripMenuItem
            // 
            this.stopServiceToolStripMenuItem.Name = "stopServiceToolStripMenuItem";
            this.stopServiceToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.stopServiceToolStripMenuItem.Text = "Communication Test";
            this.stopServiceToolStripMenuItem.Click += new System.EventHandler(this.stopServiceToolStripMenuItem_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Corbel", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(167, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 14);
            this.label3.TabIndex = 29;
            this.label3.Text = "label3";
            // 
            // btn_DMS
            // 
            this.btn_DMS.Location = new System.Drawing.Point(427, 8);
            this.btn_DMS.Name = "btn_DMS";
            this.btn_DMS.Size = new System.Drawing.Size(75, 23);
            this.btn_DMS.TabIndex = 31;
            this.btn_DMS.Text = "DMS";
            this.btn_DMS.UseVisualStyleBackColor = true;
            this.btn_DMS.Visible = false;
            this.btn_DMS.Click += new System.EventHandler(this.btn_DMS_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Corbel", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(167, 24);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 14);
            this.label4.TabIndex = 33;
            this.label4.Text = "label4";
            // 
            // pictureBox_DMS
            // 
            this.pictureBox_DMS.Image = global::Freya.Properties.Resources.dms_disable;
            this.pictureBox_DMS.Location = new System.Drawing.Point(70, 3);
            this.pictureBox_DMS.Name = "pictureBox_DMS";
            this.pictureBox_DMS.Size = new System.Drawing.Size(24, 24);
            this.pictureBox_DMS.TabIndex = 32;
            this.pictureBox_DMS.TabStop = false;
            this.pictureBox_DMS.Click += new System.EventHandler(this.pictureBox_DMS_Click);
            // 
            // label_IMAPQuota
            // 
            this.label_IMAPQuota.BackColor = System.Drawing.Color.Transparent;
            this.label_IMAPQuota.Font = new System.Drawing.Font("Corbel", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_IMAPQuota.ForeColor = System.Drawing.Color.Gray;
            this.label_IMAPQuota.Image = global::Freya.Properties.Resources.QuotaUnAvailable;
            this.label_IMAPQuota.Location = new System.Drawing.Point(36, 0);
            this.label_IMAPQuota.Margin = new System.Windows.Forms.Padding(3);
            this.label_IMAPQuota.Name = "label_IMAPQuota";
            this.label_IMAPQuota.Size = new System.Drawing.Size(28, 28);
            this.label_IMAPQuota.TabIndex = 30;
            this.label_IMAPQuota.Text = "-\r\n";
            this.label_IMAPQuota.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label_IMAPQuota.Click += new System.EventHandler(this.label_IMAPQuota_Click);
            // 
            // pictureBox_Miner
            // 
            this.pictureBox_Miner.Location = new System.Drawing.Point(100, 3);
            this.pictureBox_Miner.Name = "pictureBox_Miner";
            this.pictureBox_Miner.Size = new System.Drawing.Size(24, 24);
            this.pictureBox_Miner.TabIndex = 27;
            this.pictureBox_Miner.TabStop = false;
            // 
            // pictureBox_Service
            // 
            this.pictureBox_Service.Image = global::Freya.Properties.Resources.Service_NotInstalled;
            this.pictureBox_Service.Location = new System.Drawing.Point(6, 3);
            this.pictureBox_Service.Name = "pictureBox_Service";
            this.pictureBox_Service.Size = new System.Drawing.Size(24, 24);
            this.pictureBox_Service.TabIndex = 26;
            this.pictureBox_Service.TabStop = false;
            // 
            // Btn_Options
            // 
            this.Btn_Options.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Options.BackColor = System.Drawing.SystemColors.Window;
            this.Btn_Options.FlatAppearance.BorderColor = System.Drawing.SystemColors.Window;
            this.Btn_Options.FlatAppearance.BorderSize = 0;
            this.Btn_Options.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.Btn_Options.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ButtonFace;
            this.Btn_Options.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Btn_Options.Image = global::Freya.Properties.Resources.settings;
            this.Btn_Options.Location = new System.Drawing.Point(576, 3);
            this.Btn_Options.Name = "Btn_Options";
            this.Btn_Options.Size = new System.Drawing.Size(32, 32);
            this.Btn_Options.TabIndex = 25;
            this.toolTip.SetToolTip(this.Btn_Options, "Settings");
            this.Btn_Options.UseVisualStyleBackColor = false;
            this.Btn_Options.Click += new System.EventHandler(this.Btn_Options_Click);
            // 
            // btn_DecryptFile
            // 
            this.btn_DecryptFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_DecryptFile.BackColor = System.Drawing.SystemColors.Window;
            this.btn_DecryptFile.FlatAppearance.BorderColor = System.Drawing.SystemColors.Window;
            this.btn_DecryptFile.FlatAppearance.BorderSize = 0;
            this.btn_DecryptFile.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btn_DecryptFile.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ButtonFace;
            this.btn_DecryptFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_DecryptFile.Image = ((System.Drawing.Image)(resources.GetObject("btn_DecryptFile.Image")));
            this.btn_DecryptFile.Location = new System.Drawing.Point(546, 3);
            this.btn_DecryptFile.Name = "btn_DecryptFile";
            this.btn_DecryptFile.Size = new System.Drawing.Size(32, 32);
            this.btn_DecryptFile.TabIndex = 34;
            this.toolTip.SetToolTip(this.btn_DecryptFile, "Decrypt File");
            this.btn_DecryptFile.UseVisualStyleBackColor = false;
            this.btn_DecryptFile.Click += new System.EventHandler(this.btn_DecryptFile_Click);
            // 
            // Freya
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(614, 361);
            this.Controls.Add(this.btn_DecryptFile);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.pictureBox_DMS);
            this.Controls.Add(this.btn_DMS);
            this.Controls.Add(this.label_IMAPQuota);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox_Miner);
            this.Controls.Add(this.pictureBox_Service);
            this.Controls.Add(this.Btn_Options);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBox1);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(630, 400);
            this.Name = "Freya";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Freya";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Freya_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.contextMenuStrip_NotifyIcon.ResumeLayout(false);
            this.contextMenuStrip_MinerOperation.ResumeLayout(false);
            this.contextMenuStrip_ServiceControl.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_DMS)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Miner)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Service)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_NotifyIcon;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Button Btn_Options;
        private System.Windows.Forms.PictureBox pictureBox_Service;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.PictureBox pictureBox_Miner;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_MinerOperation;
        private System.Windows.Forms.ToolStripMenuItem enableToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disableToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_ServiceControl;
        private System.Windows.Forms.ToolStripMenuItem startServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopServiceToolStripMenuItem;
        internal System.Windows.Forms.ToolStripMenuItem alwaysActiveToolStripMenuItem;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label_IMAPQuota;
        private System.Windows.Forms.Button btn_DMS;
        private System.Windows.Forms.PictureBox pictureBox_DMS;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btn_DecryptFile;
    }
}