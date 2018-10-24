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

namespace Freya
{
    public static class FFunc
    {
        public static object GetRegKey(string KeyName)
        {
            try
            {
                return Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Freya", KeyName, null);
            }
            catch
            {
                return null;
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

        public static void GetSettingsFromRegistry(FRegSetting RegSetting)
        {
            RegSetting.EMail = (string)FFunc.GetRegKey("EMail");
            RegSetting.SMTPServerIP = (string)FFunc.GetRegKey("SmtpServerIp");
            RegSetting.WebServiceIP = (string)FFunc.GetRegKey("WebService");
            RegSetting.SMTPLogLevel = (string)FFunc.GetRegKey("SMTPLogLevel");

            RegSetting.LogLevel = (FConstants.FreyaLogLevel)FFunc.GetRegKey("LogLevel");
            RegSetting.FeatureByte = (FConstants.FeatureByte)Convert.ToInt32(FFunc.GetRegKey("FeatureByte"));
        }

        public static bool HasRight(FConstants.FeatureByte target)
        {
            FConstants.FeatureByte id = (FConstants.FeatureByte)Convert.ToInt32(FFunc.GetRegKey("FeatureByte"));

            return ((id & target) == target);
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
                    p.WaitForExit(); //設定要等待相關的處理序結束的時間，並且阻止目前的執行緒執行，直到等候時間耗盡或者處理序已經結束為止。 
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Freya need to be run as Administrator to work.\n " + ex.ToString());
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
                return false;
        }
    }
}
