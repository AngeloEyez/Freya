namespace FSLib.App.SimpleUpdater.Defination
{
	using System;
	using System.Xml.Serialization;

	using global::SimpleUpdater.Attributes;

	/// <summary> ��ʾ�����ļ���Ϣ </summary>
	/// <remarks></remarks>
	[Serializable]
	[DoNotObfuscate, DoNotObfuscateControlFlow, DoNotObfuscateType, DoNotPrune, DoNotPruneType]
	[DoNotCaptureFields, DoNotCaptureVariables, DoNotEncodeStrings]	//��ֹSmartAssembly����
	public class PackageInfo
	{
		#region ����Ϣ-��Ҫ����ĳ־û���Ϣ

		/// <summary> �ļ�·�� </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string FilePath { get; set; }

		/// <summary> �ļ���С </summary>
		/// <value></value>
		/// <remarks></remarks>
		public long FileSize { get; set; }

		/// <summary> �汾 </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string Version { get; set; }

		/// <summary> Hash </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string PackageHash { get; set; }

		/// <summary> ���� </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string PackageName { get; set; }

		/// <summary> ѹ�����ļ���С </summary>
		/// <value></value>
		/// <remarks></remarks>
		public long PackageSize { get; set; }

		/// <summary> ����ģʽ </summary>
		/// <value></value>
		/// <remarks></remarks>
		public UpdateMethod Method { get; set; }

		/// <summary> ��û����õ�ǰ�ļ���֤�ȼ� </summary>
		/// <value></value>
		/// <remarks></remarks>
		public FileVerificationLevel VerificationLevel { get; set; }

		/// <summary> ��û����ñ����ļ��Ĺ�ϣֵ </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string FileHash { get; set; }

		/// <summary>
		/// ��û����ù������ļ�
		/// </summary>
		public string[] Files { get; set; }

		/// <summary>
		/// ���ܱ�ǡ�
		/// </summary>
		public string ComponentId { get; set; }


		#endregion
		
		#region ������Ĺ�������

		/// <summary> ��ѹ�� </summary>
		public void Extract()
		{
		}

		/// <summary> ����ʧ�ܼ��� </summary>
		public void IncreaseFailureCounter()
		{
			RetryCount = (RetryCount ?? 0) + 1;
		}

		#endregion
		
		#region ��չ����-Ϊ������ʱ�����룬�ǹ̻����������е�����

		/// <summary> ��û����ô����õ������Ļ��� </summary>
		/// <value></value>
		/// <remarks></remarks>
		[System.Xml.Serialization.XmlIgnore]
		public UpdateContext Context { get; set; }


		/// <summary> ��õ�ǰ���Ƿ��������� </summary>
		/// <value></value>
		/// <remarks></remarks>
		[XmlIgnore]
		public bool IsDownloading { get; internal set; }

		/// <summary> ��õ�ǰ���Ƿ��Ѿ����� </summary>
		/// <value></value>
		/// <remarks></remarks>
		[XmlIgnore]
		public bool IsDownloaded { get; internal set; }

		/// <summary> ��ô��������������Ĵ��� </summary>
		/// <value></value>
		/// <remarks></remarks>
		[XmlIgnore]
		public Exception LastError { get; internal set; }

		/// <summary> ������Դ������� </summary>
		/// <value></value>
		/// <remarks></remarks>
		[XmlIgnore]
		public int? RetryCount { get; internal set; }

		/// <summary> ��ñ��ر���·�� </summary>
		/// <value></value>
		/// <remarks></remarks>
		[System.Xml.Serialization.XmlIgnore]
		public string LocalSavePath
		{
			get
			{
				if (Context == null) throw new InvalidOperationException("��δ���ӵ���������");
				return System.IO.Path.Combine(Context.UpdatePackagePath, PackageName);
			}
		}

		string _sourceUri;

		/// <summary> ������ص�ԴURL </summary>
		/// <value></value>
		/// <remarks></remarks>
		[XmlIgnore]
		public string SourceUri
		{
			get
			{
				if (Context == null) throw new InvalidOperationException("��δ���ӵ���������");

				return _sourceUri ?? (_sourceUri = Context.GetUpdatePackageFullUrl(PackageName));
			}
		}

		bool? _hashResult;

		/// <summary> ��ñ��صİ��ļ��Ƿ���Ч </summary>
		/// <value></value>
		/// <remarks></remarks>
		[XmlIgnore]
		public bool? IsLocalFileValid
		{
			get
			{
				var path = LocalSavePath;
				if (!System.IO.File.Exists(path)) return null;
				return _hashResult ?? (_hashResult = Wrapper.ExtensionMethod.GetFileHash(path) == PackageHash);
			}
		}

		/// <summary> ��������صĳ��� </summary>
		/// <value></value>
		/// <remarks></remarks>
		[XmlIgnore]
		public long DownloadedSize { get; internal set; }

		#endregion

		#region ��������

		/// <summary>
		/// ȷ���Ƿ��д˱��λ
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		internal bool HasVerifyFlag(FileVerificationLevel level)
		{
			return (level & VerificationLevel) > 0;
		}

		#endregion
	}
}