using System.Diagnostics;
using System.Data;
using System.Collections;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;
using System.IO;

namespace FSLib.App.SimpleUpdater.Wrapper
{
	/// <summary>
	/// XML���л�֧����
	/// </summary>
	public static class XMLSerializeHelper
	{
		/// <summary>
		/// ���л�����Ϊ�ı�
		/// </summary>
		/// <returns>������Ϣ�� <see cref="T:System.String"/></returns>
		public static T XmlDeserializeFromString<T>(string content) where T : class
		{
			if (String.IsNullOrEmpty(content))
				return null;

			using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)))
			{
				try
				{
					var xso = new XmlSerializer(typeof(T));
					return (T)xso.Deserialize(ms);
				}
				catch (Exception ex)
				{
					Trace.TraceInformation("ִ�з����л�ʱ�������� ----> \r\n" + ex.ToString());
					return default(T);
				}
			}
		}

		/// <summary>
		/// ���л������ļ�
		/// </summary>
		/// <param name="objectToSerialize">Ҫ���л��Ķ���</param>
		/// <param name="fileName">���浽��Ŀ���ļ�</param>
		public static void XmlSerilizeToFile(object objectToSerialize, string fileName)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(fileName));

			using (var stream = new FileStream(fileName, FileMode.Create))
			{
				var xso = new XmlSerializer(objectToSerialize.GetType());
				xso.Serialize(stream, objectToSerialize);
				stream.Close();
			}
		}

	}
}
