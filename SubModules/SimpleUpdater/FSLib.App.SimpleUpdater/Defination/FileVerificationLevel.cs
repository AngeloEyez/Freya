namespace FSLib.App.SimpleUpdater.Defination
{
	using System;

	/// <summary> �ļ���֤�ȼ� </summary>
	/// <remarks></remarks>
	[Flags]
	public enum FileVerificationLevel
	{
		/// <summary>
		/// û��
		/// </summary>
		None = 0,
		/// <summary> ��֤��С </summary>
		/// <remarks></remarks>
		Size = 1,
		/// <summary> ��֤�汾 </summary>
		/// <remarks></remarks>
		Version = 2,
		/// <summary> ��֤Hash </summary>
		/// <remarks></remarks>
		Hash = 4
	}
}