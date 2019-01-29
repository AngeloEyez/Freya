/*
 * OpaqueMail Proxy (https://opaquemail.org/).
 * 
 * Licensed according to the MIT License (http://mit-license.org/).
 * 
 * Copyright © Bert Johnson (https://bertjohnson.com/) of Allcloud Inc. (https://allcloud.com/).
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 */

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Freya
{
    static class Program
    {
        [DllImport("User32.dll")]
        private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        /// <summary>
        /// 根据窗口标题查找窗体
        /// </summary>
        /// <param name="lpClassName"></param>
        /// <param name="lpWindowName"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 根据句柄查找进程ID
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("User32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //If already running another process, bring to front and exit myself.
            string ProcessName = Process.GetCurrentProcess().ProcessName;
            IntPtr hWnd = new IntPtr(0);
            using (Process process = ProcessGet(ProcessName))
                if (process != null)
                {
                    try
                    {
                        IntPtr h = process.MainWindowHandle;
                        if (h.ToInt32() == 0)
                        {
                            h = FindWindow(null, "Freya" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString());

                            int id = -1;
                            GetWindowThreadProcessId(h, out id);
                            if (id == process.Id)
                                hWnd = h;
                        }
                        else
                            hWnd = h;
                    }
                    catch { }
                }

            if (hWnd != new IntPtr(0))
            {
                SwitchToThisWindow(hWnd, true);
                Environment.Exit(1);
            }


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Freya());
        }

        static Process ProcessGet(string processNameToGet)
        {
            if (processNameToGet == "")
                return null;

            int ProcessID = Process.GetCurrentProcess().Id;

            Process[] processes = Process.GetProcessesByName(processNameToGet); ;
            foreach (Process process in processes)
                if (process.Id != ProcessID)
                    return process;

            return null;
        }

    }
}
