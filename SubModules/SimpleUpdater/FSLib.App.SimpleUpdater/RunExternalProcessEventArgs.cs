using System;
using System.Diagnostics;

namespace FSLib.App.SimpleUpdater
{
	/// <summary>
	/// �ⲿ�����������¼�����
	/// </summary>
	public class RunExternalProcessEventArgs : EventArgs
	{
		/// <summary> ��ý�Ҫ�����Ľ����������� </summary>
		/// <value></value>
		/// <remarks></remarks>
		public ProcessStartInfo ProcessStartInfo { get; private set; }

		/// <summary>
		/// ���� <see cref="RunExternalProcessEventArgs" />  ����ʵ��(RunExternalProcessEventArgs)
		/// </summary>
		public RunExternalProcessEventArgs(ProcessStartInfo processStartInfo) { ProcessStartInfo = processStartInfo; }
	}
}