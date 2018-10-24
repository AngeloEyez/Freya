﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FSLib.App.SimpleUpdater.Dialogs;

namespace FSLib.App.SimpleUpdater
{
	/// <summary>
	/// 更新程序的主入口点
	/// </summary>
	public static class Program
	{
		public static bool IsRunning = false;

		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			IsRunning = true;

			if (args.Length == 0)
			{
				Wrapper.FunctionalForm.Information(SR.DonotRunMeDirectly);
				return;
			}
			if (args[0] == "/selfupdate")
			{
				new Dialogs.SelfUpdate().ShowDialog();
				return;
			}

			var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var arg in args)
			{
				if (!dic.ContainsKey(arg)) dic.Add(arg, null);
			}

			var updater = Updater.Instance;
			if (updater.Context.HiddenUI)
			{
				new HiddenUiUpdateProxy().RunUpdate();
			}
			else
			{
				if (string.IsNullOrEmpty(updater.Context.UpdateMainFormType))
				{
					Application.Run(new MainWindow());
				}
				else
				{
					var form = Activator.CreateInstance(Type.GetType(updater.Context.UpdateMainFormType)) as AbstractUpdateBase;
					Application.Run(form);
				}
			}
		}
	}
}
