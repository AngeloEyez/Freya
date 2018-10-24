﻿using System;
using System.Windows.Forms;

namespace FSLib.App.SimpleUpdater.Dialogs
{
	public partial class MainWindow : AbstractUpdateBase
	{
		public MainWindow()
		{
			InitializeComponent();

			Load += MainWindow_Load;
		}

		private void MainWindow_Load(object sender, EventArgs e)
		{
			HideAllControls();
		}

		/// <summary>
		/// 隐藏所有控件
		/// </summary>
		internal void HideAllControls()
		{
			foreach (Control item in panMain.Controls)
			{
				if (item is UpdateControl.ControlBase) item.Visible = false;
			}
		}
	}
}
