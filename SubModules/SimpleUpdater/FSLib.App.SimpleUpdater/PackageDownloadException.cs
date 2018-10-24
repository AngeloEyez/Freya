using System;

namespace FSLib.App.SimpleUpdater
{
	using Defination;

	/// <summary>
	///     ���°����ش����쳣
	/// </summary>
	[Serializable]
	public class PackageDownloadException : System.ApplicationException, System.Runtime.Serialization.ISerializable
	{

		/// <summary>
		///     Parameterless (default) constructor
		/// </summary>
		public PackageDownloadException(params PackageInfo[] packages)
			: base("����������ʧ��")
		{
		}


		/// <summary> ��ó�����ļ��� </summary>
		/// <value></value>
		/// <remarks></remarks>
		public PackageInfo[] ErrorPackages { get; private set; }
	}
}