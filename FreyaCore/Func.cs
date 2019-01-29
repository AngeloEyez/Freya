using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using ZetaIpc.Runtime.Client;

namespace Freya
{
    public static class FFunc
    {
        public static object GetRegKey(string KeyName)
        {
            object ob = new object();
            try
            {
                ob = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Freya", KeyName, null);
                return ob;
            }
            catch
            {
                ob = null;
                return ob;
            }
        }

        public static bool SetRegKey(string KeyName, object KeyValue)
        {
            try
            {
                RegistryKey Reg = Registry.LocalMachine.OpenSubKey("Software", true);

                ////檢查子機碼是否存在，檢查資料夾是否存在。
                if (Reg.GetSubKeyNames().Contains("Freya") == false)
                {
                    Reg.CreateSubKey("Freya"); //建立子機碼，建立資料夾。
                }

                //寫入資料 Name,Value,"寫入類型"
                Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Freya", KeyName, KeyValue);
                Reg.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasRight(FConstants.FeatureByte target)
        {
            FConstants.FeatureByte fb = (FConstants.FeatureByte)Convert.ToInt32(FFunc.GetRegKey("FeatureByte"));

            return ((fb & target) == target);
        }

        public static void DelRight(FConstants.FeatureByte target)
        {
            FConstants.FeatureByte fb = (FConstants.FeatureByte)Convert.ToInt32(FFunc.GetRegKey("FeatureByte"));
            fb = (fb & (FConstants.FeatureByte.ALL ^ target)); //刪除
            FFunc.SetRegKey("FeatureByte", Convert.ToInt32(fb));
        }

        public static void AddRight(FConstants.FeatureByte target)
        {
            FConstants.FeatureByte fb = (FConstants.FeatureByte)Convert.ToInt32(FFunc.GetRegKey("FeatureByte"));
            fb = (fb | target); //增加
            FFunc.SetRegKey("FeatureByte", Convert.ToInt32(fb));
        }
        

        public static void SingleInstance()
        {
            string ProcessName = Process.GetCurrentProcess().ProcessName;
            Process[] p = Process.GetProcessesByName(ProcessName);
            if (p.Length > 1)
            {
                Console.WriteLine("Duplicated run.");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 從URL抓取JSON並deserialize to Object. [[ var currencyRates = _download_serialized_json_data&lt;CurrencyRates&gt;(url) ]]
        /// </summary>
        public static T DownloadJsonToObject<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;
                // attempt to download JSON data as a string
                try
                {
                    json_data = w.DownloadString(url);
                }
                catch (Exception) { }
                // if string with JSON data is not empty, deserialize it to class and return its instance 
                return !string.IsNullOrEmpty(json_data) ? JsonConvert.DeserializeObject<T>(json_data) : new T();
            }
        }

        public static bool Heimdallr(string cmd)
        {
            string target = System.IO.Path.Combine(Application.StartupPath, @"heimdallr.exe");
            target = System.IO.Path.GetFullPath(target);
            if (File.Exists(target))
            {
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = target,
                        Arguments = cmd,
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false
                    },
                    //EnableRaisingEvents = true,
                };
                try
                {
                    p.Start();
                    //p.WaitForInputIdle(); //讓 Process 元件等候相關的處理序進入閒置狀態。 
                    //p.WaitForExit(); //設定要等待相關的處理序結束的時間，並且阻止目前的執行緒執行，直到等候時間耗盡或者處理序已經結束為止。 
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Freya need to be run as Administrator to work.\r\nPlease click yes if ask for permission." + ex.ToString());
                    return false;
                }

                if (p != null)
                {
                    p.Close();
                    p.Dispose();
                    p = null;
                }

                return true;
            }
            else
            {
                MessageBox.Show("Can't find heimdallr.exe.\r\nCheck your installation, make sure all file exists.");
                return false;
            }
        }

        public static class FreePortHelper
        {
            private static readonly Random Random = new Random(Guid.NewGuid().GetHashCode());
            public static readonly List<int> ReservedPorts = new List<int>();

            /// <summary>
            /// Gets a free port on the current machine.
            /// </summary>
            public static int GetFreePort(int startPort = 9000)
            {
                if (startPort > 64000)
                    startPort -= 1000;
                else if (startPort < 6000)
                    startPort = 6000;

                for (var i = 0; i < 500; ++i)
                {
                    var port = Random.Next(startPort, startPort + 1000);
                    if (isPortFree(port))
                    {
                        ReservedPorts.Add(port);
                        return port;
                    }
                }

                throw new Exception("Unable to acquire free port.");
            }

            private static bool isPortFree(int port)
            {
                if (ReservedPorts.Contains(port))
                {
                    return false;
                }
                else
                {
                    // http://stackoverflow.com/a/570126/107625

                    var globalProperties = IPGlobalProperties.GetIPGlobalProperties();
                    var informations = globalProperties.GetActiveTcpListeners();

                    return informations.All(information => information.Port != port);
                }
            }
        }

        public static string[] GetWorkerRuntimeName()
        {
            string[] workerRuntimeName = new string[FConstants.WorkerFileName.Length];
            for (int i = 0; i<FConstants.WorkerFileName.Length; i++)
            {
                workerRuntimeName[i] = FConstants.WorkerFileName[i].Substring(0, FConstants.WorkerFileName[i].LastIndexOf(".")) + "." + FConstants.WorkerExtentionString;
            }
            return workerRuntimeName;
        }

        public static string GetSizeString(ulong bytes)
        {
            string transmittedStr;
            if (bytes < 1024)
                transmittedStr = string.Format("{0:#} bytes", bytes);
            else if (bytes >= 1024 && bytes < 1048576)
                transmittedStr = string.Format("{0:#.#} Kbs", bytes / 1024);
            else if (bytes >= 1048576 && bytes < 1073741824)
                transmittedStr = string.Format("{0:#.##} Mbs", bytes / 1024/1024);
            else
                transmittedStr = string.Format("{0:#.##} Gbs", bytes / 1024 / 1024 / 1024);
            return transmittedStr;
        }
    }


    public class FIpcClient : IpcClient
    {
        public string Send(string cmd, string data, string data2 = null)
        {
            if (data2 != null)
                return base.Send(JsonConvert.SerializeObject(new FMsg { Type = cmd, Data = data, Data2 = data2 }));
            else
                return base.Send(JsonConvert.SerializeObject(new FMsg { Type = cmd, Data = data }));
        }
    }
}
