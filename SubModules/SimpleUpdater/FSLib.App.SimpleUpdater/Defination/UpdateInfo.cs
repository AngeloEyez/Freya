﻿namespace FSLib.App.SimpleUpdater.Defination
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Text.RegularExpressions;

	using Annotations;

	using global::SimpleUpdater.Attributes;

	using Wrapper;

	/// <summary>
	/// 升级信息的具体包装
	/// </summary>
	[Serializable]
	[DoNotObfuscate, DoNotObfuscateControlFlow, DoNotObfuscateType, DoNotPrune, DoNotPruneType]
	[DoNotCaptureFields, DoNotCaptureVariables, DoNotEncodeStrings] //防止SmartAssembly处理
	public class UpdateInfo : INotifyPropertyChanged
	{
		public UpdateInfo()
		{
			AutoKillProcesses = false;
			AutoEndProcessesWithinAppDir = true;
			ForceUpdate = false;
			MustUpdate = false;
			PromptUserBeforeAutomaticUpgrade = true;
		}

		/// <summary>
		/// 应用程序名
		/// </summary>
		public string AppName
		{
			get { return _appName; }
			set
			{
				if (value == _appName)
					return;
				_appName = value;
				OnPropertyChanged("AppName");
			}
		}

		/// <summary>
		/// 应用程序版本
		/// </summary>
		public string AppVersion
		{
			get { return _appVersion; }
			set
			{
				if (value == _appVersion)
					return;
				_appVersion = value;
				OnPropertyChanged("AppVersion");
			}
		}

		/// <summary>
		/// 发布页面地址
		/// </summary>
		public string PublishUrl
		{
			get { return _publishUrl; }
			set
			{
				if (value == _publishUrl)
					return;
				_publishUrl = value;
				OnPropertyChanged("PublishUrl");
			}
		}

		/// <summary>
		/// 更新前执行的程序
		/// </summary>
		public string FileExecuteBefore
		{
			get { return _fileExecuteBefore; }
			set
			{
				if (value == _fileExecuteBefore)
					return;
				_fileExecuteBefore = value;
				OnPropertyChanged("FileExecuteBefore");
			}
		}

		/// <summary>
		/// 更新前执行的程序参数
		/// </summary>
		public string ExecuteArgumentBefore
		{
			get { return _executeArgumentBefore; }
			set
			{
				if (value == _executeArgumentBefore)
					return;
				_executeArgumentBefore = value;
				OnPropertyChanged("ExecuteArgumentBefore");
			}
		}

		/// <summary>
		/// 更新后执行的程序
		/// </summary>
		public string FileExecuteAfter
		{
			get { return _fileExecuteAfter; }
			set
			{
				if (value == _fileExecuteAfter)
					return;
				_fileExecuteAfter = value;
				OnPropertyChanged("FileExecuteAfter");
			}
		}

		/// <summary>
		/// 更新后执行的程序参数
		/// </summary>
		public string ExecuteArgumentAfter
		{
			get { return _executeArgumentAfter; }
			set
			{
				if (value == _executeArgumentAfter)
					return;
				_executeArgumentAfter = value;
				OnPropertyChanged("ExecuteArgumentAfter");
			}
		}

		/// <summary>
		/// 程序执行超时
		/// </summary>
		public int ExecuteTimeout
		{
			get { return _executeTimeout; }
			set
			{
				if (value == _executeTimeout)
					return;
				_executeTimeout = value;
				OnPropertyChanged("ExecuteTimeout");
			}
		}

		private string _desc;
		private List<PackageInfo> _packages;
		string _appName;
		string _appVersion;
		string _publishUrl;
		string _fileExecuteBefore;
		string _executeArgumentBefore;
		string _fileExecuteAfter;
		string _executeArgumentAfter;
		int _executeTimeout;
		string _package;
		string _md5;
		string _requiredMinVersion;
		long _packageSize;
		Version _updaterVersion;
		string _packagePassword;
		Version _updateManifestVersion;
		bool _forceUpdate;
		bool _forceKillProcesses;
		bool _hideBeforeExecuteWindow;
		bool _hideAfterExecuteWindow;
		string _webUpdateNote;
		string _rtfUpdateNote;
		bool _mustUpdate;
		string _updatePingUrl;
		string _packageUrlTemplate;
		string[] _serverCluster;
		bool _autoExitCurrentProcess;
		bool _autoKillProcesses;
		bool _autoEndProcessesWithinAppDir;
		bool _treatErrorAsNotUpdated;
		bool _promptUserBeforeAutomaticUpgrade;
		bool _requreAdminstrorPrivilege;


		/// <summary>
		/// 设置当出现错误的时候，是否按照有更新但是未更新处理。
		/// 这个选项影响设置必须强制更新的选项。
		/// 如果检测更新遇到错误，此选项设置为false时，则按照“未找到更新”处理；如果此选项设置为true，则按照“有更新但是没有更新”处理，会强制退出软件。
		/// </summary>
		public bool TreatErrorAsNotUpdated
		{
			get { return _treatErrorAsNotUpdated; }
			set
			{
				if (value.Equals(_treatErrorAsNotUpdated)) return;
				_treatErrorAsNotUpdated = value;
				OnPropertyChanged("TreatErrorAsNotUpdated");
			}
		}

		/// <summary>
		/// 获得或设置是否在正式进行更新前先通知用户
		/// </summary>
		public bool PromptUserBeforeAutomaticUpgrade
		{
			get { return _promptUserBeforeAutomaticUpgrade; }
			set
			{
				if (value.Equals(_promptUserBeforeAutomaticUpgrade)) return;
				_promptUserBeforeAutomaticUpgrade = value;
				OnPropertyChanged("PromptUserBeforeAutomaticUpgrade");
			}
		}

		bool _autoCloseSucceedWindow = true;

		/// <summary>
		/// 获得或设置当升级成功后是否自动关闭提示对话框
		/// </summary>
		public bool AutoCloseSucceedWindow
		{
			get { return _autoCloseSucceedWindow; }
			set
			{
				if (value.Equals(_autoCloseSucceedWindow)) return;
				_autoCloseSucceedWindow = value;
				OnPropertyChanged("AutoCloseSucceedWindow");
			}
		}

		/// <summary>
		/// 更新描述
		/// </summary>
		public string Desc
		{
			get
			{
				return _desc;
			}
			set
			{
				_desc = string.Join(Environment.NewLine, value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				OnPropertyChanged("Desc");
			}
		}

		/// <summary>
		/// 安装包文件名
		/// </summary>
		public string Package
		{
			get { return _package; }
			set
			{
				if (value == _package)
					return;
				_package = value;
				OnPropertyChanged("Package");
			}
		}

		/// <summary>
		/// 校验的HASH
		/// </summary>
		public string MD5
		{
			get { return _md5; }
			set
			{
				if (value == _md5)
					return;
				_md5 = value;
				OnPropertyChanged("MD5");
			}
		}

		/// <summary>
		/// 要删除或要保留的文件
		/// </summary>
		public string[] DeleteFileLimits { get; set; }

		/// <summary>
		/// 获得删除规则的正则表达式形式
		/// </summary>
		/// <returns></returns>
		internal List<Regex> GetDeleteFileLimitRuleSet()
		{
			if (this.DeleteFileLimits == null) return new List<Regex>();
			return ExtensionMethod.ToList(Wrapper.ExtensionMethod.Select(DeleteFileLimits, s => new Regex(s, RegexOptions.IgnoreCase)));
		}

		/// <summary>
		/// 删除方式
		/// </summary>
		public DeletePreviousProgramMethod DeleteMethod { get; set; }

		#region new property in 1.3.0.0

		/// <summary>
		/// 升级需要的最低版本
		/// </summary>
		public string RequiredMinVersion
		{
			get { return _requiredMinVersion; }
			set
			{
				if (value == _requiredMinVersion)
					return;
				_requiredMinVersion = value;
				OnPropertyChanged("RequiredMinVersion");
			}
		}

		/// <summary>
		/// 包大小
		/// </summary>
		public long PackageSize
		{
			get { return _packageSize; }
			set
			{
				if (value == _packageSize)
					return;
				_packageSize = value;
				OnPropertyChanged("PackageSize");
			}
		}

		#endregion

		#region new property in 1.5.0.0

		/// <summary>
		/// 升级程序版本
		/// </summary>
		public Version UpdaterVersion
		{
			get { return _updaterVersion; }
			set
			{
				if (Equals(value, _updaterVersion))
					return;
				_updaterVersion = value;
				OnPropertyChanged("UpdaterVersion");
			}
		}

		/// <summary>
		/// 升级包密码
		/// </summary>
		public string PackagePassword
		{
			get { return _packagePassword; }
			set
			{
				if (value == _packagePassword)
					return;
				_packagePassword = value;
				OnPropertyChanged("PackagePassword");
			}
		}

		#endregion

		#region new property in 2.0.0.0

		/// <summary>获得当前更新文件的版本</summary>
		/// <value></value>
		/// <remarks></remarks>
		public Version UpdateManifestVersion
		{
			get { return _updateManifestVersion; }
			set
			{
				if (Equals(value, _updateManifestVersion))
					return;
				_updateManifestVersion = value;
				OnPropertyChanged("UpdateManifestVersion");
			}
		}

		/// <summary> 是否不提示用户便强制升级 </summary>
		/// <value></value>
		/// <remarks></remarks>
		public bool ForceUpdate
		{
			get { return _forceUpdate; }
			set
			{
				if (value.Equals(_forceUpdate))
					return;
				_forceUpdate = value;
				OnPropertyChanged("ForceUpdate");
			}
		}

		/// <summary> 获得或设置更新包集合 </summary>
		/// <value></value>
		/// <remarks></remarks>
		public List<PackageInfo> Packages
		{
			get { return _packages ?? (_packages = new List<PackageInfo>()); }
			set { _packages = value; }
		}

		/// <summary>
		/// 隐藏更新前执行进程的窗口
		/// </summary>
		public bool HideBeforeExecuteWindow
		{
			get { return _hideBeforeExecuteWindow; }
			set
			{
				if (value.Equals(_hideBeforeExecuteWindow))
					return;
				_hideBeforeExecuteWindow = value;
				OnPropertyChanged("HideBeforeExecuteWindow");
			}
		}

		/// <summary>
		/// 隐藏更新后执行的进程窗口
		/// </summary>
		public bool HideAfterExecuteWindow
		{
			get { return _hideAfterExecuteWindow; }
			set
			{
				if (value.Equals(_hideAfterExecuteWindow))
					return;
				_hideAfterExecuteWindow = value;
				OnPropertyChanged("HideAfterExecuteWindow");
			}
		}

		#endregion

		#region new property in 2.2.0.0

		/// <summary>
		/// 获得或设置升级说明的网页路径
		/// </summary>
		public string WebUpdateNote
		{
			get { return _webUpdateNote; }
			set
			{
				if (value == _webUpdateNote)
					return;
				_webUpdateNote = value;
				OnPropertyChanged("WebUpdateNote");
			}
		}

		/// <summary>
		/// 获得或设置RTF格式的升级说明
		/// </summary>
		public string RtfUpdateNote
		{
			get { return _rtfUpdateNote; }
			set
			{
				if (value == _rtfUpdateNote)
					return;
				_rtfUpdateNote = value;
				OnPropertyChanged("RtfUpdateNote");
			}
		}

		#endregion

		#region new property in 2.3.0.0

		/// <summary>
		/// 获得或设置是否必须进行升级,否则拒绝运行
		/// </summary>
		public bool MustUpdate
		{
			get { return _mustUpdate; }
			set
			{
				if (value.Equals(_mustUpdate))
					return;
				_mustUpdate = value;
				OnPropertyChanged("MustUpdate");
			}
		}

		/// <summary>
		/// 获得或设置是否自动退出当前进程
		/// </summary>
		public bool AutoExitCurrentProcess
		{
			get { return _autoExitCurrentProcess; }
			set
			{
				if (value.Equals(_autoExitCurrentProcess)) return;
				_autoExitCurrentProcess = value;
				OnPropertyChanged("AutoExitCurrentProcess");
			}
		}

		/// <summary>
		/// 获得或设置一个值，指示着当自动更新的时候是否将应用程序目录中的所有进程都作为主进程请求结束
		/// </summary>
		public bool AutoEndProcessesWithinAppDir
		{
			get { return _autoEndProcessesWithinAppDir; }
			set
			{
				if (value.Equals(_autoEndProcessesWithinAppDir)) return;
				_autoEndProcessesWithinAppDir = value;
				OnPropertyChanged("AutoEndProcessesWithinAppDir");
			}
		}


		/// <summary>
		/// 获得或设置是否在更新时自动结束进程
		/// </summary>
		public bool AutoKillProcesses
		{
			get { return _autoKillProcesses; }
			set
			{
				if (value.Equals(_autoKillProcesses)) return;
				_autoKillProcesses = value;
				OnPropertyChanged("AutoKillProcesses");
			}
		}


		/// <summary>
		/// 获得可用于更新的服务器集群列表
		/// </summary>
		public string[] ServerCluster
		{
			get { return _serverCluster; }
			set
			{
				if (Equals(value, _serverCluster))
					return;
				_serverCluster = value;
				OnPropertyChanged("ServerCluster");
			}
		}

		/// <summary>
		/// 获得或设置在进行更新前发送响应的地址
		/// </summary>
		public string UpdatePingUrl
		{
			get { return _updatePingUrl; }
			set
			{
				if (value == _updatePingUrl)
					return;
				_updatePingUrl = value;
				OnPropertyChanged("UpdatePingUrl");
			}
		}

		/// <summary>
		///  获得用于下载文件包的模板URL。为空则使用默认的
		/// </summary>
		public string PackageUrlTemplate
		{
			get { return _packageUrlTemplate; }
			set
			{
				if (value == _packageUrlTemplate)
					return;
				_packageUrlTemplate = value;
				OnPropertyChanged("PackageUrlTemplate");
			}
		}

		/// <summary>
		/// 强行请求Administrator权限
		/// </summary>
		public bool RequreAdminstrorPrivilege
		{
			get { return _requreAdminstrorPrivilege; }
			set
			{
				if (value.Equals(_requreAdminstrorPrivilege)) return;
				_requreAdminstrorPrivilege = value;
				OnPropertyChanged("RequreAdminstrorPrivilege");
			}
		}

		#endregion

		#region 受保护函数

		/// <summary>
		/// 
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="propertyName"></param>
		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
