﻿namespace FSLib.App.SimpleUpdater.Dialogs
{
	partial class UpdateFound
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateFound));
			this.lblFound = new System.Windows.Forms.Label();
			this.btnUpdate = new System.Windows.Forms.Button();
			this.lnkSoft = new System.Windows.Forms.LinkLabel();
			this.lblSize = new System.Windows.Forms.Label();
			this.lblVersion = new System.Windows.Forms.Label();
			this.controlContainer = new System.Windows.Forms.Panel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.lnkCance = new System.Windows.Forms.LinkLabel();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblFound
			// 
			resources.ApplyResources(this.lblFound, "lblFound");
			this.lblFound.ForeColor = System.Drawing.Color.White;
			this.lblFound.Name = "lblFound";
			// 
			// btnUpdate
			// 
			resources.ApplyResources(this.btnUpdate, "btnUpdate");
			this.btnUpdate.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(145)))), ((int)(((byte)(45)))));
			this.btnUpdate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(189)))), ((int)(((byte)(232)))), ((int)(((byte)(174)))));
			this.btnUpdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(121)))), ((int)(((byte)(146)))), ((int)(((byte)(63)))));
			this.btnUpdate.Image = global::FSLib.App.SimpleUpdater.Properties.Resources.cou_32_refresh;
			this.btnUpdate.Name = "btnUpdate";
			this.btnUpdate.UseVisualStyleBackColor = true;
			this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
			// 
			// lnkSoft
			// 
			resources.ApplyResources(this.lnkSoft, "lnkSoft");
			this.lnkSoft.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
			this.lnkSoft.LinkColor = System.Drawing.Color.Green;
			this.lnkSoft.Name = "lnkSoft";
			this.lnkSoft.TabStop = true;
			// 
			// lblSize
			// 
			resources.ApplyResources(this.lblSize, "lblSize");
			this.lblSize.Name = "lblSize";
			// 
			// lblVersion
			// 
			this.lblVersion.ForeColor = System.Drawing.Color.White;
			resources.ApplyResources(this.lblVersion, "lblVersion");
			this.lblVersion.Name = "lblVersion";
			// 
			// controlContainer
			// 
			resources.ApplyResources(this.controlContainer, "controlContainer");
			this.controlContainer.Name = "controlContainer";
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(147)))), ((int)(((byte)(180)))), ((int)(((byte)(101)))));
			this.panel1.Controls.Add(this.lblVersion);
			this.panel1.Controls.Add(this.lblFound);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.ForeColor = System.Drawing.SystemColors.ControlText;
			this.panel1.Name = "panel1";
			// 
			// lnkCance
			// 
			this.lnkCance.Image = global::FSLib.App.SimpleUpdater.Properties.Resources.block_16;
			resources.ApplyResources(this.lnkCance, "lnkCance");
			this.lnkCance.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
			this.lnkCance.LinkColor = System.Drawing.Color.Green;
			this.lnkCance.Name = "lnkCance";
			this.lnkCance.TabStop = true;
			this.lnkCance.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkCance_LinkClicked);
			// 
			// UpdateFound
			// 
			this.AcceptButton = this.btnUpdate;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.Controls.Add(this.lnkCance);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.controlContainer);
			this.Controls.Add(this.lblSize);
			this.Controls.Add(this.lnkSoft);
			this.Controls.Add(this.btnUpdate);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "UpdateFound";
			this.Load += new System.EventHandler(this.UpdateFound_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblFound;
		private System.Windows.Forms.Button btnUpdate;
		private System.Windows.Forms.LinkLabel lnkSoft;
		private System.Windows.Forms.Label lblSize;
		private System.Windows.Forms.Label lblVersion;
		private System.Windows.Forms.Panel controlContainer;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.LinkLabel lnkCance;
	}
}