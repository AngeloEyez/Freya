using System;

namespace FSLib.App.SimpleUpdater
{
	/// <summary>
	/// ��ʾ�ڶ����滻ģʽ���Զ����±��
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	public class Updatable2Attribute : Attribute
	{
		/// <summary> �����������ģ�� </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string UrlTemplate { get; private set; }

		/// <summary> ���������Ϣ�ļ��� </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string InfoFileName { get; private set; }


		/// <summary>
		/// ���� <see cref="Updatable2Attribute" />  ����ʵ��(Updatable2Attribute)
		/// </summary>
		/// <param name="urlTemplate">�����ļ���URLģ�壬�� {0} Ϊռλ��</param>
		/// <param name="infoFileName">����������Ϣ���ļ���</param>
		public Updatable2Attribute(string urlTemplate, string infoFileName)
		{
			UrlTemplate = urlTemplate;
			InfoFileName = infoFileName;
		}
	}
}