using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Freya
{
    public class LogWriter
    {
        private string m_Path = string.Empty;
        private bool m_enableTextLog = false;

        public LogWriter(bool enableTextLog = false)
        {
            m_enableTextLog = enableTextLog;
            m_Path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Logs\\Freya.log";

            WriteLine("------------------------------------------------");
        }
        public void WriteLine(string logMessage)
        {
            if (m_enableTextLog)
            {
                EnsureDirectory(m_Path);
                try
                {
                    using (StreamWriter w = File.AppendText(m_Path))
                    {
                        w.WriteLine("{0:yyyy-MM-dd HH:mm:ss} - {1}", DateTime.Now, logMessage);
                    }
                }
                catch { }
            }
            Console.WriteLine(logMessage);
        }

        public void Write(string logMessage, bool notag = false)
        {
            if (m_enableTextLog)
            {
                EnsureDirectory(m_Path);
                try
                {
                    using (StreamWriter w = File.AppendText(m_Path))
                    {
                        if (notag)
                            w.Write("{0}\r\n", logMessage);
                        else
                            w.Write("{0:yyyy-MM-dd HH:mm:ss} - {1}", DateTime.Now, logMessage);
                    }
                }
                catch { }
            }
            Console.Write(logMessage + ((notag) ? "\r\n" : ""));
        }

        protected static void EnsureDirectory(string directory)
        {
            // Unless this is a UNC path, make sure the specified directory exists.
            if (!directory.StartsWith("\\"))
            {
                string[] pathParts = directory.Split('\\');
                string path = pathParts[0];

                for (int i = 1; i < pathParts.Length - 1; i++)
                {
                    path += "\\" + pathParts[i];
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
            }
        }

    }

}
