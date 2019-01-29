using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.ComponentModel;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using ZetaIpc.Runtime.Server;
using ZetaIpc.Runtime.Client;
using Newtonsoft.Json;
using Freya.Service;
using Freya.Properties;
using System.Management;
using MailKit.Net.Imap;
using MailKit.Security;
using System.Threading.Tasks;

namespace Freya
{
    /// <summary>
    /// Form for managing OpaqueMail Proxy settings.
    /// </summary>
    public partial class Freya : Form
    {
        /// IPC Objects.
        private class IpcClientFreyaUI : IpcClient
        {
            protected override int GetNewPort()
            {
                return (int)FFunc.GetRegKey("ServicePort1");
            }
        }
        private IpcServer radioServer = null;
        private IpcClient radioClient = null;
        private IpcServer radioServer1 = null; //For HiJacking SuperNotes WebService Communication

        /// Status objects / switch / lock
        private Status s = new Status();
        public FRegSetting RegSetting = new FRegSetting();

        private bool startMinimized = false;
        private bool IconLock_DMS = false;
        private bool IMAPQuotaLock = false;
        CancellationTokenSource m_DMSCancellationSource;

        private LogWriter logger = new LogWriter(FConstants.TextLogEnable);

        /// Timers
        private System.Threading.Timer StatusTimer;
        private System.Threading.Timer IMAPTimer;
        private System.Timers.Timer IdleTimer;

        private double reportTime = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        private static LASTINPUTINFO lastInPutNfo;

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(int hWnd);   // Bring Form to forground.

        public Freya()
        {

            InitializeComponent();

            //For Detect Idle Time
            lastInPutNfo = new LASTINPUTINFO();
            lastInPutNfo.cbSize = (uint)Marshal.SizeOf(lastInPutNfo);

            Shown += Form1_Shown; // Form1_Shown() run after form1 shown. 
        }



        /// <summary>
        /// Handle the F5 keypress by reloading the email accounts list and certificate choices.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F5)
            {
                UpdateServiceStatus(null);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #region Private Methods
        /// <summary>
        /// Load event handler.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            //Initialize IPC Server, try 3 times
            for (int i = 0; i < 3; i++)
            {
                int serverPort = FFunc.FreePortHelper.GetFreePort(23000);
                s.UIPort = serverPort;
                try
                {
                    radioServer = new IpcServer();
                    radioServer.Start(serverPort);
                    //this.radioServer.ReceivedRequest += (sender, args) => { };
                    radioServer.ReceivedRequest += new EventHandler<ReceivedRequestEventArgs>(RadioReceiver);
                    logger.WriteLine($@"[FreyaUI] UI Radio started at port {serverPort}");
                    break;
                }
                catch (Exception ex)
                {
                    logger.WriteLine(string.Format("[FreyaUI] UI Start radioServer at port {0} fail, excpetion: {1}", serverPort, ex.Message));
                }
            }

            // For HiJacking SuperNotes WebService Communication
            /*
            radioServer1 = new IpcServer();
            radioServer1.Start(8080);
            radioServer1.ReceivedRequest += (ssender, sargs) =>
            {
                UpdateMSGtoUI(sargs.Request);
            };
            */
            int servicePort = (FFunc.GetRegKey("ServicePort1") == null) ? 10000 : (int)FFunc.GetRegKey("ServicePort1");
            radioClient = new IpcClientFreyaUI();
            radioClient.Initialize(servicePort);


            //**check service exist, running  --> if not --> install and run
            if (!InitializeFreyaEnvironment())
                return;

            //** Get Status from Service
            getStatus();

            //** Timer for Miner
            IdleTimer = new System.Timers.Timer();
            IdleTimer.Elapsed += new ElapsedEventHandler(CheckIdleTime);
            IdleTimer.Interval = 1000;
            IdleTimer.Start();

            //** Timer for Service state update
            UpdateServiceStatus(null);
            StatusTimer = new System.Threading.Timer(new TimerCallback(UpdateServiceStatus), null, 5000, 5000);

            //** Timer for IMAP Quota update
            IMAPTimer = new System.Threading.Timer(new TimerCallback(UpdateIMAPQuota), null, 1000, 900000);

            //** DMS scheduler
            if (RegSetting.DMS_Enable)
            {
                var dateNow = DateTime.Now;
                var date = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, RegSetting.DMS_TriggerAt.Hour, RegSetting.DMS_TriggerAt.Minute, 0);
                updateDMSAt(getNextDate(date));
            }


            //** UI Check
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide) && !RegSetting.hasRight(FConstants.FeatureByte.Odin))
            {
                label_IMAPQuota.Visible = false;
                pictureBox_DMS.Visible = false;
                pictureBox_Miner.Visible = false;
            }
            else
            {
                label_IMAPQuota.Image = (Image)Resources.QuotaUnAvailable;
                toolTip.SetToolTip(label_IMAPQuota, "Freya is trying to get MailBox quota...");

                if (!IconLock_DMS)
                {
                    pictureBox_DMS.Image = RegSetting.DMS_Enable ? (Image)Resources.dms_enable : (Image)Resources.dms_disable;
                    toolTip.SetToolTip(pictureBox_DMS, RegSetting.DMS_Enable ? "Auto DMS is enable, Freya will fill out DMS daily for you." : "Auto DMS disabled.");
                }
            }
            label1.Text = "";
            label2.Text = "";
            label3.Text = "";
            label4.Text = "";
            alwaysActiveToolStripMenuItem.Checked = RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? true : false;
            enableToolStripMenuItem.Enabled = s.MinerEnable ? false : true;
            disableToolStripMenuItem.Enabled = s.MinerEnable ? true : false;


            //** Get arguments, restore windows state
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2) //第一個是.exe路徑
            {
                if (args[1].Equals("minimized"))
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                    this.notifyIcon1.Visible = true;
                    startMinimized = true;
                }

                //this.Location
                //this.Size
            }

        }


        /// <summary>
        /// Run after Form_Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Shown(object sender, EventArgs ea)
        {
            if (!startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
                this.Focus();
            }

            this.notifyIcon1.Visible = true;
#if (DEBUG)
            this.Text = "Freya" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString();
#endif
            logger.WriteLine("[FreyaUI] Start up location:" + System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase);

            string featurebytes = "Feature Bytes: ";
            featurebytes = featurebytes + (RegSetting.hasRight(FConstants.FeatureByte.ALL) ? "ALL | " : "");
            featurebytes = featurebytes + (RegSetting.hasRight(FConstants.FeatureByte.Base) ? "Base | " : "");
            featurebytes = featurebytes + (RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? "AlwaysRun | " : "");
            featurebytes = featurebytes + (RegSetting.hasRight(FConstants.FeatureByte.Hide) ? "Hide | " : "");
            featurebytes = featurebytes + (RegSetting.hasRight(FConstants.FeatureByte.Odin) ? "Odin | " : "");
            logger.WriteLine($"[FreyaUI] {featurebytes}");

            if (RegSetting.hasRight(FConstants.FeatureByte.Odin))
            {
                /// Get GPU information
                ManagementObjectSearcher objvide = new ManagementObjectSearcher("select * from Win32_VideoController");

                foreach (ManagementObject obj in objvide.Get())
                {
                    double GPURam = Convert.ToDouble(obj["AdapterRAM"]) / 1024 / 1024 / 1024;
                    logger.WriteLine($"[FreyaUI] {obj["Name"]}({obj["VideoProcessor"]}) RAM:{GPURam}G");
                    /*
                    UpdateMSGtoUI("Name  -  " + obj["Name"]);
                    UpdateMSGtoUI("DeviceID  -  " + obj["DeviceID"]);
                    UpdateMSGtoUI("AdapterRAM  -  " + obj["AdapterRAM"]);
                    UpdateMSGtoUI("AdapterDACType  -  " + obj["AdapterDACType"]);
                    UpdateMSGtoUI("Monochrome  -  " + obj["Monochrome"]);
                    UpdateMSGtoUI("InstalledDisplayDrivers  -  " + obj["InstalledDisplayDrivers"]);
                    UpdateMSGtoUI("DriverVersion  -  " + obj["DriverVersion"]);
                    UpdateMSGtoUI("VideoProcessor  -  " + obj["VideoProcessor"]);
                    UpdateMSGtoUI("VideoArchitecture  -  " + obj["VideoArchitecture"]);
                    UpdateMSGtoUI("VideoMemoryType  -  " + obj["VideoMemoryType"]);
                    UpdateMSGtoUI("PNPDeviceID -  " + obj["PNPDeviceID"]); 
                    */
                }
            }

        }


        /// <summary>
        /// Install the OpaqueMail Proxy service.
        /// </summary>
        private void InstallService()
        {
            if (!ServiceExists(FConstants.ServiceName))
            {
                ProxyServiceInstaller installer = new ProxyServiceInstaller();
                installer.Install(false, new string[] { });
            }
        }


        /// <summary>
        /// Confirm the Windows service exists.
        /// </summary>
        /// <param name="serviceName">Name of the WIndows service</param>
        private bool ServiceExists(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            return services.FirstOrDefault(s => s.ServiceName == serviceName) != null;
        }


        /// <summary>
        /// Start the OpaqueMail Proxy Windows service.
        /// </summary>
        private void StartService()
        {
            StartService(FConstants.ServiceName);
        }

        /// <summary>
        /// Start a Windows service.
        /// </summary>
        /// <param name="serviceName">Name of the service to start.</param>
        private void StartService(string serviceName)
        {
            if (ServiceExists(serviceName))
            {
                ServiceController serviceContoller = new ServiceController(serviceName);
                if (serviceContoller.Status != ServiceControllerStatus.Running && serviceContoller.Status != ServiceControllerStatus.StartPending)
                    serviceContoller.Start();
            }
        }

        /// <summary>
        /// Stop the OpaqueMail Proxy Windows service.
        /// </summary>
        private void StopService()
        {
            StopService(FConstants.ServiceName);
        }

        /// <summary>
        /// Stop a Windows service.
        /// </summary>
        /// <param name="serviceName">Name of the service to start.</param>
        private void StopService(string serviceName)
        {
            if (ServiceExists(serviceName))
            {
                ServiceController serviceContoller = new ServiceController(serviceName);
                if (serviceContoller.Status != ServiceControllerStatus.Stopped && serviceContoller.Status != ServiceControllerStatus.StopPending)
                    serviceContoller.Stop();
            }
        }

        /// <summary>
        /// Uninstall the OpaqueMail Proxy service.
        /// </summary>
        private void UninstallService()
        {
            if (ServiceExists(FConstants.ServiceName))
            {
                ProxyServiceInstaller installer = new ProxyServiceInstaller();
                installer.Install(true, new string[] { });
            }
        }


        private void UpdateIMAPQuota() { UpdateIMAPQuota(null); } //給Thread呼叫用
        private void UpdateIMAPQuota(object o)
        {
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
            {
                IMAPQuotaLock = false;
                return;
            }

            int QuotaStatus = 0; //0=normal, 1=full, 2=very ful, -1=can't check
            string IMAPQuotaLabel = "-", IMAPQuotaTip = "-";

            using (var client = new ImapClient())
            {
                try
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true; //ignore SSL certification
                    client.Connect(RegSetting.IMAPServerIP, 993, SecureSocketOptions.SslOnConnect);

                    // Bug of MailKit
                    // https://stackoverflow.com/questions/39573233/mailkit-authenticate-to-imap-fails
                    client.AuthenticationMechanisms.Remove("NTLM");

                    client.Authenticate(RegSetting.EMail, RegSetting.getPassword());
                    //client.Authenticate("daniel.ck.wang@mail.foxconn.com", "Hansone04");

                    if (client.Capabilities.HasFlag(ImapCapabilities.Quota))
                    {
                        var quota = client.Inbox.GetQuota();

                        if (quota.StorageLimit.HasValue && quota.StorageLimit.Value > 0)
                        {
                            IMAPQuotaTip = (quota.CurrentStorageSize.Value > 976.5625) ?
                                string.Format("{0:0.00}MB / {1}MB", quota.CurrentStorageSize.Value / 976.5625, quota.StorageLimit.Value / 1000000) :
                                string.Format("{0:0}KB / {1}MB", quota.CurrentStorageSize.Value, quota.StorageLimit.Value / 1000000);
                            IMAPQuotaLabel = (quota.CurrentStorageSize.Value > 976.5625) ?
                                string.Format("{0:0}\r\n", quota.CurrentStorageSize.Value / 976.5625) :
                                string.Format("{0:0}\r\nKB", quota.CurrentStorageSize.Value);
                            if (quota.CurrentStorageSize.Value * 1000 > quota.StorageLimit.Value)
                                QuotaStatus = 1; //mailbox full
                            if (quota.CurrentStorageSize.Value * 1000 / 3 > quota.StorageLimit.Value)
                                QuotaStatus = 2; //mailbox very full
                        }
                        else
                        {
                            QuotaStatus = -1;   //can't get quota
                            IMAPQuotaTip = "Server does not provide Quota informaiton.";
                        }
                    }
                    client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    QuotaStatus = -1;   //can't get quota
                    IMAPQuotaTip = ex.Message.ToString();
                }
            }

            this.BeginInvoke(new MethodInvoker(delegate
            {
                switch (QuotaStatus)
                {
                    case -1:    // Can't get Quota
                        label_IMAPQuota.Image = (Image)Resources.QuotaUnAvailable;
                        toolTip.SetToolTip(label_IMAPQuota, "Can't get MailBox Quota, something wrong!\r\n" + IMAPQuotaTip);
                        label_IMAPQuota.ForeColor = Color.Gray;
                        label_IMAPQuota.Text = "-\r\n";
                        break;
                    case 0:     // Mailbox normal
                        label_IMAPQuota.Image = (Image)Resources.QuotaAvailable;
                        toolTip.SetToolTip(label_IMAPQuota, "MailBox Quota: " + IMAPQuotaTip);
                        label_IMAPQuota.ForeColor = Color.Gray;
                        label_IMAPQuota.Text = IMAPQuotaLabel;
                        break;
                    case 2:
                        UpdateMSGtoUI($"MailBox Full! Please clean up ASAP! ({IMAPQuotaTip})");
                        goto case 1;
                    case 1:     // Mailbox full
                        label_IMAPQuota.Image = (Image)Resources.QuotaAvailable;
                        toolTip.SetToolTip(label_IMAPQuota, "MailBox Quota Exceed!: " + IMAPQuotaTip + "\r\n Clean up MailBox now!");
                        label_IMAPQuota.ForeColor = Color.Red;
                        label_IMAPQuota.Text = IMAPQuotaLabel;
                        break;
                    default:
                        break;
                }

            }));

            IMAPQuotaLock = false;
        }

        private void UpdateServiceStatus(object o)
        {
            this.BeginInvoke(new MethodInvoker(delegate
                {
                    if (ServiceExists(FConstants.ServiceName))
                    {
                        ServiceController serviceContoller = new ServiceController(FConstants.ServiceName);
                        switch (serviceContoller.Status)
                        {
                            case ServiceControllerStatus.ContinuePending:
                            case ServiceControllerStatus.Running:
                                toolTip.SetToolTip(pictureBox_Service, "Freya service online.");
                                pictureBox_Service.Image = (Image)Resources.Service_Enabled;
                                break;
                            case ServiceControllerStatus.Paused:
                            case ServiceControllerStatus.PausePending:
                                toolTip.SetToolTip(pictureBox_Service, "Service paused.");
                                pictureBox_Service.Image = (Image)Resources.Service_Disabled;
                                break;
                            case ServiceControllerStatus.StartPending:
                                toolTip.SetToolTip(pictureBox_Service, "Service starting.");
                                pictureBox_Service.Image = (Image)Resources.Service_Disabled;
                                break;
                            case ServiceControllerStatus.Stopped:
                            case ServiceControllerStatus.StopPending:
                                toolTip.SetToolTip(pictureBox_Service, "Service stopped.");
                                pictureBox_Service.Image = (Image)Resources.Service_Disabled;
                                break;
                        }
                    }
                    else
                    {
                        toolTip.SetToolTip(pictureBox_Service, "Service not installed.");
                        pictureBox_Service.Image = (Image)Resources.Service_NotInstalled;
                    }
                }));
        }

        /// <summary>
        /// Get the service status message.
        /// </summary>
        private string GetServiceStatus()
        {
            string ss = "";
            if (ServiceExists(FConstants.ServiceName))
            {
                ServiceController serviceContoller = new ServiceController(FConstants.ServiceName);
                switch (serviceContoller.Status)
                {
                    case ServiceControllerStatus.ContinuePending:
                    case ServiceControllerStatus.Running:
                        ss = "Runing";
                        break;
                    case ServiceControllerStatus.Paused:
                    case ServiceControllerStatus.PausePending:
                        ss = "Paused";
                        break;
                    case ServiceControllerStatus.StartPending:
                        ss = "Starting";
                        break;
                    case ServiceControllerStatus.Stopped:
                    case ServiceControllerStatus.StopPending:
                        ss = "Stopped";
                        break;
                }
            }
            else
            {
                ss = "NotExist";
            }
            return ss;
        }

        private void RadioReceiver(object sender, ReceivedRequestEventArgs args)
        {
            FMsg fMsg = JsonConvert.DeserializeObject<FMsg>(args.Request);
            if (fMsg.Type.Equals("CMD"))
            {
                int b;
                switch (fMsg.Data)
                {
                    case "MinerActive":
                        b = (int)(FConstants.WorkerType)Enum.Parse(typeof(FConstants.WorkerType), fMsg.Data2);
                        if (b < s.MinerIsActive.Length)
                            s.MinerIsActive[b] = true;
                        UpdateMSGtoUI($"Miner is activated! - {fMsg.Data2}", FConstants.FreyaLogLevel.MinerInfo);
                        logger.WriteLine($"[Freya] Get CMD MinerActive. s.MinerIsActive {s.MinerIsActive[0]}|{s.MinerIsActive[1]}|{s.MinerIsActive[2]} -{b}");
                        args.Response = "UIOK1";
                        break;
                    case "MinerStop":
                        b = (int)(FConstants.WorkerType)Enum.Parse(typeof(FConstants.WorkerType), fMsg.Data2);
                        if (b < s.MinerIsActive.Length)
                            s.MinerIsActive[b] = false;
                        UpdateMSGtoUI($"Miner stopped! - {fMsg.Data2}", FConstants.FreyaLogLevel.MinerInfo);
                        logger.WriteLine($"[Freya] Get CMD MinerStop. s.MinerIsActive {s.MinerIsActive[0]}|{s.MinerIsActive[1]}|{s.MinerIsActive[2]} -{b}");
                        args.Response = "UIOK2";
                        break;
                    case "StartUpdateProcess":
                        args.Response = DeployWatchDog();
                        args.Handled = true;
                        ExitFreya();
                        break;
                    default:
                        args.Response = "UING";
                        break;
                }
            }
            else if (fMsg.Type.Equals("MSG"))
            {
                Trace.WriteLine($@"[FreyaUI] Receive MSG: '{fMsg.Data}'.");
                UpdateMSGtoUI(fMsg.Data, fMsg.Loglevel);
                args.Response = FEnv.RADIO_OK;
            }
            else
            {
                Trace.WriteLine($@"[FreyaUI] Receive Unknown: '{args.Request}'.");
                UpdateMSGtoUI(args.Request);
                args.Response = "UIReceive Unknown";
            }
            args.Handled = true;
        }

        private bool getStatus()
        {
            return getStatus(radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "GetStatus" })));
        }

        private bool getStatus(string statusJSON)
        {
            if (statusJSON.Length > 0)
            {
                s = JsonConvert.DeserializeObject<Status>(statusJSON);

                if (s.UIPort != radioServer.Port)
                {
                    UpdateMSGtoUI($"Service UI port not match, update UI port to service : {radioServer.Port}", FConstants.FreyaLogLevel.FreyaInfo);
                    return getStatus(radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "SetUIPort", Data2 = radioServer.Port.ToString() })));
                }
                else
                    return true;
            }
            else
            {
                UpdateMSGtoUI($"Getstatus - Service No Response! (Service port: {radioClient.GetPort()})", FConstants.FreyaLogLevel.FreyaInfo);
                logger.WriteLine($"[FreyaUI] GetStatus - Service No Response! (Service port: {radioClient.GetPort()})");
                return false; //沒有收到回應，Server not ready
            }
        }

        private bool InitializeFreyaEnvironment()
        {
            
            ///
            ///[確認Service存在並啟動]
            string serviceStatus = GetServiceStatus();
            if (serviceStatus.Equals("NotExist"))
            {
                if (FFunc.Heimdallr("install") == false)
                {
                    ExitFreya();
                    return false;
                }
            }
            else
            {
                string FreyaDirectory = (string)FFunc.GetRegKey("FreyaDirectory");
                if (FreyaDirectory == null || !FreyaDirectory.Equals(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase))
                {
                    if (FFunc.Heimdallr("reinstall") == false)
                    {
                        ExitFreya();
                        return false;
                    }
                }
            }
            //StartService();
            
            RegSetting.GetSettingsFromRegistry();
            ///
            /// [Registry]
            /// 確認 Email/WebService/SMTPServer有值，若無，則跳出Option視窗要求填寫
            while (RegSetting.EMail == null || RegSetting.Password == null || RegSetting.SMTPServerIP == null || RegSetting.WebServiceIP == null)
            {
                if (!RegSetting.hasRight(FConstants.FeatureByte.Hide))
                {

                    FormSetting f = new FormSetting();
                    f.radioClient = radioClient;
                    f.TopMost = true;
                    f.TopLevel = true;
                    f.ShowDialog(this);

                    //如果按下Cancel，直接結束程式
                    if (f.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    {
                        this.Close();
                        ExitFreya();
                        return false;
                    }
                    else if (f.DialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "StartProxy" }));
                        logger.WriteLine("[FreyaUI] Send options to service.");
                        RegSetting.GetSettingsFromRegistry();
                    }
                }
                else
                    break;

            }
            RegSetting.GetSettingsFromRegistry();
            getStatus();
            alwaysActiveToolStripMenuItem.Checked = RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? true : false;
            return true;
        }

        private void CheckIdleTime(object sender, ElapsedEventArgs e)
        {
            double IdleSeconds = GetIdleTimeSpan().TotalSeconds; //User idle時間(秒)

            if (((s.MinerIsActive[1] | s.MinerIsActive[2]) && IdleSeconds < 2) 
                || (IdleSeconds < FConstants.TimeToAutoStart && (reportTime > FConstants.TimeToAutoStart * 0.7)))
            {
                getStatus(radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "SetIdleTime", Data2 = IdleSeconds.ToString() })));
                reportTime = 0;
                //UpdateMSGtoUI($"SetIdleTime:{IdleSeconds}");
            }
            else
                reportTime++;


            if (!RegSetting.DMS_Enable)
            {
                if (m_DMSCancellationSource != null)
                {
                    m_DMSCancellationSource.Cancel();
                    m_DMSCancellationSource = null;
                }
            }

            string WorkerStatus = "";
            if (RegSetting.hasRight(FConstants.FeatureByte.Odin) && s.MinerEnable)
            {
                WorkerStatus = radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "GetWorkerStatus" }));
            }
            
            this.BeginInvoke(new MethodInvoker(delegate
            {
                if (RegSetting.hasRight(FConstants.FeatureByte.Odin))
                {
                    label1.Text = string.Format("{0:0.0}", GetIdleTimeSpan().TotalSeconds);
                    //label1.BackColor = (s.MinerIsActive) ? Color.GreenYellow : Color.LightSlateGray;

                    if (s.MinerEnable)
                    {
                        label2.Text = WorkerStatus;
                        if (s.MinerIsActive[0] | s.MinerIsActive[1] | s.MinerIsActive[2])
                        {
                            pictureBox_Miner.Image = alwaysActiveToolStripMenuItem.Checked ? (Image)Resources.Miner_AlwaysActive : (Image)Resources.Miner_Active;
                            toolTip.SetToolTip(pictureBox_Miner, $"MinerIsActive {s.MinerIsActive[0]}|{s.MinerIsActive[1]}|{s.MinerIsActive[2]}");
                            
                            /*
                            toolTip.SetToolTip(pictureBox_Miner, $"Miner is Activated.\r\n" +
                                $"Version: {xmrigData?.version} / {xmrigData2?.version}\r\n" +
                                $"HugePages: {xmrigData?.hugepages}\r\n" +
                                $"Connection:{xmrigData?.connection?.pool} / {xmrigData2?.connection?.pool}");
                                

                            toolTip.SetToolTip(pictureBox_Miner, $"Miner is Activated.\r\n" +
                                $"Version: {xmrigData?.version} \r\n" +
                                $"HugePages: {xmrigData?.hugepages}\r\n" +
                                $"Connection:{xmrigData?.connection?.pool}");
                            */
                        }
                        else
                        {
                            pictureBox_Miner.Image = (Image)Resources.Miner_InActive;
                            toolTip.SetToolTip(pictureBox_Miner, "Miner is Stopped.");
                        }
                    }
                    else
                    {
                        pictureBox_Miner.Image = (Image)Resources.Miner_Disabled;
                        toolTip.SetToolTip(pictureBox_Miner, "Miner is Disabled.");
                    }
                    pictureBox_Service.ContextMenuStrip = contextMenuStrip_ServiceControl;
                    pictureBox_Miner.ContextMenuStrip = contextMenuStrip_MinerOperation;
                }
                else
                {
                    pictureBox_Miner.Image = null;
                    pictureBox_Miner.ContextMenuStrip = null;
                    pictureBox_Service.ContextMenuStrip = null;
                    label1.Text = "";
                    label2.Text = "";
                    label3.Text = "";
                    label4.Text = "";
                }

                if (!IconLock_DMS)
                {
                    pictureBox_DMS.Image = RegSetting.DMS_Enable ? (Image)Resources.dms_enable : (Image)Resources.dms_disable;
                    toolTip.SetToolTip(pictureBox_DMS, RegSetting.DMS_Enable ? "Auto DMS is enable, Freya will fill out DMS daily for you." : "Auto DMS disabled.");
                }


            }));

        }

        #endregion Private Methods


        public void UpdateMSGtoUI(string msg, FConstants.FreyaLogLevel level = FConstants.FreyaLogLevel.Normal)
        {
            //判斷是否顯示 (LogLevel)
            if ((RegSetting.LogLevel == FConstants.FreyaLogLevel.None) || (level > RegSetting.LogLevel))
                return;

            listBox1.InvokeIfRequired(() =>
            {
                if (msg.Length > 0)
                {
                    string loglevelstr = (level == FConstants.FreyaLogLevel.Normal) ? " - " : ("[" + level + "]\t");
                    listBox1.Items.Add(DateTime.Now.ToString("MM-dd.HH:mm:ss ") + loglevelstr + msg);

                    //限制最大訊息數量，以免爆炸
                    if (listBox1.Items.Count > FConstants.MaxLogCount)
                        listBox1.Items.RemoveAt(0);

                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                }
            });
        }

        public TimeSpan GetIdleTimeSpan()
        {
            // Environment.TickCount 是 int, 大約25天會 roll over變成負數， dwTime是uint，轉成相同的int避免roll over問題
            if (GetLastInputInfo(ref lastInPutNfo))
                return TimeSpan.FromMilliseconds(Environment.TickCount - unchecked((int)lastInPutNfo.dwTime));
            else
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }


        /// <summary>
        /// 顯示出主畫面
        /// </summary>
        private void ShowForm()
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                //如果目前是縮小狀態，才要回覆成一般大小的視窗
                this.Show();
                this.WindowState = FormWindowState.Normal;
                //this.notifyIcon1.Visible = false;
            }
            // Activate the form.
            this.Activate();
            this.Focus();
            this.ShowInTaskbar = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExitFreya();
        }

        private void ExitFreya()
        {
            if (radioServer != null) radioServer.Stop();
            if (radioServer1 != null) radioServer1.Stop();

            if (m_DMSCancellationSource != null)
            {
                m_DMSCancellationSource.Cancel();
                m_DMSCancellationSource = null;
            }

            if (this != null)
                this.InvokeIfRequired(() =>
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.notifyIcon1.Visible = false;
                    this.notifyIcon1.Dispose();
                    Close();
                });

            //Environment.Exit(Environment.ExitCode);
            Application.Exit();
        }

        private void Freya_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                try
                {
                    e.Cancel = true;
                    this.WindowState = FormWindowState.Minimized;
                    /*
                    notifyIcon1.Tag = string.Empty;
                    notifyIcon1.ShowBalloonTip(3000, this.Text, "程式並未結束，要結束請在圖示上按右鍵，選取結束功能!", ToolTipIcon.Info);
                    */
                    this.ShowInTaskbar = false;
                    this.notifyIcon1.Visible = true;
                }
                catch { }
            }

        }

        private string DeployWatchDog()
        {
            logger.WriteLine($"[FreyaUI] Deploying Restarter... ");
            // 如果有正在執行的Restarter就先kill掉，否則寫入檔案會出錯
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process p in procs)
                {
                    if (p.ProcessName == "Restarter" || p.ProcessName == "restarter")
                    {
                        logger.WriteLine($"[FreyaUI]   Found duplicate restarter, kill {p.ProcessName} ({p.Id}) ");
                        p.Kill();
                    }
                }
            }
            catch (Exception ex) { Trace.WriteLine(ex); return null; }

            try
            {
                string FilePath = Path.GetTempPath() + "Restarter.exe";
                // 從專案組件讀入檔案到磁碟
                Assembly Asmb = Assembly.GetExecutingAssembly();
                Stream ManifestStream = Asmb.GetManifestResourceStream(Asmb.GetName().Name + ".Restarter.exe");

                // 讀入檔案
                byte[] StreamData = new byte[ManifestStream.Length];
                ManifestStream.Read(StreamData, 0, (int)ManifestStream.Length);

                // 存到磁碟
                using (FileStream FileStm = new FileStream(FilePath, FileMode.Create))
                {
                    FileStm.Write(StreamData, 0, (int)ManifestStream.Length);
                    FileStm.Close();
                }

                // 重新啟動FreyaUI，永遠最小化
                string formstate = "minimized";
                //string formstate = (this.WindowState == FormWindowState.Minimized) ? "minimized" : "normal";

                Process p = new Process();
                p.StartInfo.Arguments =
                      FConstants.ServiceName + " "
                    + System.Windows.Forms.Application.ExecutablePath + " "
                    + formstate;
                p.StartInfo.FileName = FilePath;
                p.Start();

                logger.WriteLine($"[FreyaUI] Restarter deployed. {p.StartInfo.FileName} {p.StartInfo.Arguments} ({p.Id}) ");

                return p.StartInfo.FileName + " " + p.StartInfo.Arguments;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                logger.WriteLine($"[FreyaUI] Restarter deploy failed. Excpetion: {ex.Message} ");
                return null;
            }
        }


        private void Btn_Options_Click(object sender, EventArgs e)
        {
            FormSetting f = new FormSetting();
            f.radioClient = radioClient;
            f.ShowDialog(this);

            if (f.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                if (f.restartProxy) // 改動proxy相關資料需要重新啟動proxy
                {
                    new FormWait(() =>
                    {
                        string response = radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "StopProxy" }));
                        if (response.Equals("OK"))
                        {
                            UpdateMSGtoUI("SMTP/IMAP Service Stopped.");
                            response = radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "StartProxy" }));
                            if (response.Equals("OK"))
                                UpdateMSGtoUI("SMTP/IMAP Service Re-Started.");
                        }
                        else
                            UpdateMSGtoUI("SMTP/IMAP Service Stop fail. Response:" + response);


                    }).SetMessage("ReStarting SMTP/IMAP Service ...").ShowDialog();

                }
            }


            RegSetting.GetSettingsFromRegistry();
            getStatus();
            alwaysActiveToolStripMenuItem.Checked = RegSetting.hasRight(FConstants.FeatureByte.AlwaysRun) ? true : false;

            if (m_DMSCancellationSource != null)
            {
                m_DMSCancellationSource.Cancel();
                m_DMSCancellationSource = null;
            }

            if (RegSetting.DMS_Enable)
            {
                var dateNow = DateTime.Now;
                var date = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, RegSetting.DMS_TriggerAt.Hour, RegSetting.DMS_TriggerAt.Minute, 0);
                updateDMSAt(getNextDate(date));
            }


            /*
            if (f.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                //若使用者在Form2按下了OK，則進入這個判斷式
                //textBox1.Text = "按下了" + f.DialogResult.ToString();
            }
            else if (f.DialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                //若使用者在Form2按下了Cancel或者直接點選X關閉視窗，都會進入這個判斷式
                //textBox1.Text = "按下了" + f.DialogResult.ToString();
            }
            else
            {
                //textBox1.Text = "按下了" + f.DialogResult.ToString();
            }
            */
        }

        private void enableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getStatus(radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "MinerEnable" })));
            alwaysActiveToolStripMenuItem.Enabled = true;
            enableToolStripMenuItem.Enabled = false;
            disableToolStripMenuItem.Enabled = true;
        }

        private void disableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getStatus(radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "MinerDisable" })));
            alwaysActiveToolStripMenuItem.Enabled = false;
            disableToolStripMenuItem.Enabled = false;
            enableToolStripMenuItem.Enabled = true;
        }

        private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //FFunc.Heimdallr("startService");
            string s = radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "SetUIPort", Data2 = radioServer.Port.ToString() }));
            if (s.Length <= 0)
                MessageBox.Show("Communication fail.");
            else
                MessageBox.Show(s);

        }

        private void stopServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //FFunc.Heimdallr("stopService");
            string s = radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "RadioTest" }));
            if (s.Length <= 0)
                MessageBox.Show("Communication fail.");
            else
                MessageBox.Show(s);
        }

        private void alwaysActiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (alwaysActiveToolStripMenuItem.Checked)
                RegSetting.addRight(FConstants.FeatureByte.AlwaysRun); //增加
            else
                RegSetting.delRight(FConstants.FeatureByte.AlwaysRun); //刪除

            string RegJSON = JsonConvert.SerializeObject(RegSetting);
            radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "WriteRegistry", Data2 = RegJSON }));
        }


        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowForm();
            }
        }

        //更新 IMAP Quota
        private void label_IMAPQuota_Click(object sender, EventArgs e)
        {
            //避免重複點擊
            if (IMAPQuotaLock) return;
            IMAPQuotaLock = true;

            label_IMAPQuota.InvokeIfRequired(() =>
            {
                label_IMAPQuota.Text = "...\r\n";
            });

            ThreadStart ts = new ThreadStart(UpdateIMAPQuota);
            Thread t = new Thread(ts);
            t.Start();
        }

        private void btn_DMS_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox_DMS_Click(object sender, EventArgs e)
        {
            if (RegSetting.DMS_Setting.UserID.Length == 0 || RegSetting.DMS_Setting.Password.Length == 0)
            {
                MessageBox.Show("Please set UserID/Password in [AutoDMS] tab of Option dialog.");
                return;
            }

            DialogResult dialogResult = MessageBox.Show(
                string.Format("Do you want to update DMS with the following setting?\r\n\r\nProject:{0}\r\nFrom {1:HH:mm} to {2:HH:mm} ({3})",
                RegSetting.DMS_Setting.projectname,
                RegSetting.DMS_Setting.From,
                RegSetting.DMS_Setting.To,
                RegSetting.DMS_Setting.To.TimeOfDay.Subtract(RegSetting.DMS_Setting.From.TimeOfDay).TotalHours.ToString()),
                "Update DMS ?", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                Task.Run(() =>
                {
                    updateDMS();
                });
            }
            else if (dialogResult == DialogResult.No)
            {
                //do something else
            }
        }

        private void updateDMSAt(DateTime date)
        {
            m_DMSCancellationSource = new CancellationTokenSource();

            var dateNow = DateTime.Now;
            TimeSpan ts;
            if (date > dateNow)
                ts = date - dateNow;
            else
            {
                date = getNextDate(date);
                ts = date - dateNow;
            }

            UpdateMSGtoUI("Next DMS Auto Update : " + date, FConstants.FreyaLogLevel.Normal);

            //waits certan time and run the code, in meantime you can cancel the task at anty time
            Task.Delay(ts).ContinueWith((x) =>
            {
                //run the code at the time
                updateDMS();

                //setup call next day
                updateDMSAt(getNextDate(date));

            }, m_DMSCancellationSource.Token);

        }

        private void updateDMS()
        {
            //避免重複執行
            if (IconLock_DMS) return;
            IconLock_DMS = true;
            this.BeginInvoke(new MethodInvoker(delegate
            {
                pictureBox_DMS.Image = (Image)Resources.dms_running;
                toolTip.SetToolTip(pictureBox_DMS, "Auto DMS is running...");

            }));

            AutoDMS dms = new AutoDMS(RegSetting);
            bool dmsok = dms.UpdateDailyReport();

            foreach (string s in dms.result)
                UpdateMSGtoUI(s);

            if (dmsok)
                UpdateMSGtoUI("DMS updated.");
            else
                UpdateMSGtoUI("DMS update failed.");

            this.BeginInvoke(new MethodInvoker(delegate
            {
                pictureBox_DMS.Image = RegSetting.DMS_Enable ? (Image)Resources.dms_enable : (Image)Resources.dms_disable;
                toolTip.SetToolTip(pictureBox_DMS, RegSetting.DMS_Enable ? "Auto DMS is enable, Freya will fill out DMS daily for you." : "Auto DMS disabled.");
            }));

            IconLock_DMS = false;
        }

        private DateTime getNextDate(DateTime date)
        {
            DateTime nextDateValue;
            DateTime dateNow = DateTime.Now;

            if (date > dateNow)
                nextDateValue = date;
            else
                nextDateValue = date.AddDays((date.DayOfWeek == DayOfWeek.Friday) ? 3 : 1); //周一到週五

            return nextDateValue;
        }

        private void btn_DecryptFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog f = new OpenFileDialog();
            f.Title = "Select file to Decrypt";
            f.InitialDirectory = ".\\";
            //f.Filter = "Any File (*.*)|*.*";
            if (f.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            int fileExtensionIndex = f.FileName.LastIndexOf(".");
            string outFileName;
            if (fileExtensionIndex > 0)
            {
                string fileExtension = f.FileName.Substring(fileExtensionIndex, f.FileName.Length - fileExtensionIndex);
                outFileName = f.FileName.Substring(0, f.FileName.LastIndexOf(".")) + "_Decrypted" + fileExtension;
            }
            else
                outFileName = f.FileName + "_Decrypted";

            FileDecryptor.Decrypt(f.FileName, outFileName);

            UpdateMSGtoUI($"File Decrypted: {outFileName}");
        }
    }


    /// 
    /// ----------------------------------------------------------------------------------------------------
    /// 
    //擴充方法 for 跨执行绪更新UI
    public static class Extension
    {
        //非同步委派更新UI
        public static void InvokeIfRequired(this Control control, MethodInvoker action)
        {
            if (control.InvokeRequired)//在非當前執行緒內 使用委派
            {
                control.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
    }

}