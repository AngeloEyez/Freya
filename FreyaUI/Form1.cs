using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.ComponentModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.XPath;
using ZetaIpc.Runtime.Server;
using ZetaIpc.Runtime.Client;
using Newtonsoft.Json;
using Freya.Service;

namespace Freya
{
    /// <summary>
    /// Form for managing OpaqueMail Proxy settings.
    /// </summary>
    public partial class Freya : Form
    {
        #region Private Members
        /// <summary>IPC Objects.</summary>
        private IpcServer radioServer = null;
        private IpcClient radioClient = null;

        private int IdleReportTime = 0;

        private Status s = new Status();

        public FRegSetting RegSetting = new FRegSetting();


        /// <summary>Timer to check the service status.</summary>
        private System.Threading.Timer StatusTimer;

        /// <summary>Detect user activity</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        private static LASTINPUTINFO lastInPutNfo;
        private System.Timers.Timer IdleTimer;

        #endregion Private Members

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Freya()
        {
            InitializeComponent();

            //For Detect Idle Time
            lastInPutNfo = new LASTINPUTINFO();
            lastInPutNfo.cbSize = (uint)Marshal.SizeOf(lastInPutNfo);
        }
        #endregion Constructors

        #region Protected Methods
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
        #endregion Protected Methods

        #region Private Methods
        /// <summary>
        /// Load event handler.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            //**Initialize IPC Server/Client
            radioServer = new IpcServer();
            radioServer.Start(FConstants.IPCPortMainUI);
            //this.radioServer.ReceivedRequest += (sender, args) => { };
            radioServer.ReceivedRequest += new EventHandler<ReceivedRequestEventArgs>(RadioReceiver);

            radioClient = new IpcClient();
            radioClient.Initialize(FConstants.IPCPortService1);

            //**check service exist, running  --> if not --> install and run
            InitializeFreyaEnvironment();

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

            //** UI Check
            this.Text = "Freya" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString();
            if (FFunc.HasRight(FConstants.FeatureByte.Dark))
            {
                this.button5.Visible = false;
                this.button6.Visible = false;
                this.button8.Visible = false;
            }
            else
            {
                this.button5.Visible = true;
                this.button6.Visible = true;
                this.button8.Visible = true;
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

        /// <summary>
        /// Update the service status message.
        /// </summary>
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
                                ServiceStatusLabel.Text = "Service running successfully.";
                                ServiceStatusLabel.ForeColor = Color.DarkGreen;
                                break;
                            case ServiceControllerStatus.Paused:
                            case ServiceControllerStatus.PausePending:
                                ServiceStatusLabel.Text = "Service paused.";
                                ServiceStatusLabel.ForeColor = Color.Black;
                                break;
                            case ServiceControllerStatus.StartPending:
                                ServiceStatusLabel.Text = "Service starting.";
                                ServiceStatusLabel.ForeColor = Color.Black;
                                break;
                            case ServiceControllerStatus.Stopped:
                            case ServiceControllerStatus.StopPending:
                                ServiceStatusLabel.Text = "Service stopped.";
                                ServiceStatusLabel.ForeColor = Color.Black;
                                break;
                        }
                    }
                    else
                    {
                        ServiceStatusLabel.Text = "Service not installed.";
                        ServiceStatusLabel.ForeColor = Color.DarkRed;
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
                switch (fMsg.Data)
                {
                    case "MinerActive":
                        s.MinerIsActive = true;
                        label1.BackColor = Color.GreenYellow;
                        UpdateMSGtoUI("Miner is activated!", FConstants.FreyaLogLevel.MinerInfo);
                        args.Response = "UIOK1";
                        break;
                    case "MinerStop":
                        s.MinerIsActive = false;
                        label1.BackColor = Color.LightSlateGray;
                        UpdateMSGtoUI("Miner stopped!", FConstants.FreyaLogLevel.MinerInfo);
                        args.Response = "UIOK2";
                        break;
                    default:
                        args.Response = "UING";
                        break;
                }
            }
            else if (fMsg.Type.Equals("MSG"))
            {
                UpdateMSGtoUI(fMsg.Data, fMsg.Loglevel);
                args.Response = "UIOK3";
            }
            else
            {
                UpdateMSGtoUI(args.Request);
                args.Response = "UIOK4";
            }
            args.Handled = true;
        }

        private bool getStatus()
        {
            string statusJSON = radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "GetStatus" }));
            if (statusJSON.Length > 0)
            {
                s = JsonConvert.DeserializeObject<Status>(statusJSON);
                //UpdateMSGtoUI("[MinerU] Getstatus - MinerIsActive: " + s.MinerIsActive.ToString(), FConstants.FreyaLogLevel.MinerInfo);
                return true;
            }
            else
            {
                UpdateMSGtoUI("[Freya] Getstatus - No Response!", FConstants.FreyaLogLevel.FreyaInfo);
                return false; //沒有收到回應，Server not ready
            }
        }

        private void InitializeFreyaEnvironment()
        {
            ///
            ///[確認Service存在並啟動]
            string serviceStatus = GetServiceStatus();
            if (!serviceStatus.Equals("Runing")) // Service尚未安装, 從確認registry設定開始,並安裝service
            {
                FFunc.Heimdallr("install");
            }

            ///
            /// [Registry]
            /// 確認 Email/WebService/SMTPServer有值，若無，則跳出Option視窗要求填寫
            while (FFunc.GetRegKey("EMail") == null || FFunc.GetRegKey("SmtpServerIp") == null || FFunc.GetRegKey("WebService") == null)
            {
                if (FFunc.HasRight(FConstants.FeatureByte.Dark))
                    break;
                FormSetting f = new FormSetting();
                f.radioClient = radioClient;
                f.ShowDialog(this);

                //如果按下Cancel，直接結束程式
                if (f.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                {
                    this.Close();
                    Environment.Exit(Environment.ExitCode);
                }
                else if (f.DialogResult == System.Windows.Forms.DialogResult.OK)
                    radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "StartProxy" }));
            }
            FFunc.GetSettingsFromRegistry(RegSetting);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            button6.Enabled = false;
            radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "MinerDisable" }));
            getStatus();
            button6.Enabled = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "MinerEnable" }));
            getStatus();
            button5.Enabled = true;
        }

        private void CheckIdleTime(object sender, ElapsedEventArgs e)
        {
            string msg = "";
            double IdleSeconds = GetIdleTimeSpan().TotalSeconds; //User idle時間(秒)
            if (s.MinerIsActive)
            {
                if (!FFunc.HasRight(FConstants.FeatureByte.Dark) && IdleSeconds < FConstants.TimeIdleThreshold) //離開Idle馬上停止Miner
                {
                    radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "IDLESTOP" }));
                    IdleReportTime = 0;
                    getStatus();
                    msg = "[MinerU] Idle disable (MinerIsActive=" + s.MinerIsActive.ToString() + ")";
                }
            }
            else
            {
                if (s.MinerEnable && IdleSeconds > FConstants.TimeIdleThreshold) //Idle超過threshold啟動Miner
                {
                    radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "IDLEGO" }));
                    IdleReportTime = 0;
                    msg = "[MinerU] Idle超過threshold啟動Miner";
                    getStatus();
                }
                else if ((IdleReportTime * IdleTimer.Interval / 1000) > FConstants.TimeToAutoStart - 30) //最遲在TimeToAutoStart-30秒report Idle狀態
                {
                    radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "IDLESTOP" }));
                    IdleReportTime = 0;
                    msg = "[MinerU] Report Idle status";
                    getStatus();
                }
                else
                    IdleReportTime++;
            }

            UpdateMSGtoUI(msg, FConstants.FreyaLogLevel.MinerInfo);

            label1.InvokeIfRequired(() =>
            {
                label1.Text = "Idle Time: " + GetIdleTimeSpan().TotalSeconds + " / Idle Report Time: " + (IdleReportTime * (IdleTimer.Interval / 1000));
                label1.BackColor = (s.MinerIsActive) ? Color.GreenYellow : Color.LightSlateGray;
            });

            label3.InvokeIfRequired(() =>
            {
                label3.Text = s.MinerEnable ? "Enable" : "Disabled";
            });

            /// Update Miner status
            /// 
            if (RegSetting.LogLevel >= FConstants.FreyaLogLevel.MinerInfo)
            {
                JSONXmrig.Rootobject xmrigData = FFunc.DownloadJsonToObject<JSONXmrig.Rootobject>("http://127.0.0.1:" + FConstants.MinerAPIPort.ToString());
                UpdateMSGtoUI(string.Format("[Miner] Version: {0}", xmrigData.version), FConstants.FreyaLogLevel.MinerInfo);
            }
           
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
                    listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff ") + "[" + level + "]- " + msg);

                    //限制最大訊息數量，以免爆炸
                    if (listBox1.Items.Count > FConstants.MaxLogCount)
                        listBox1.Items.RemoveAt(0);

                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                }
            });
        }

        public TimeSpan GetIdleTimeSpan()
        {
            if (GetLastInputInfo(ref lastInPutNfo))
                return TimeSpan.FromMilliseconds(Environment.TickCount - lastInPutNfo.dwTime);
            else
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }



        private void button8_Click(object sender, EventArgs e)
        {
            RegistryKey Reg = Registry.LocalMachine.OpenSubKey("Software", true);
            //刪除 子機碼，刪除資料夾
            Reg.DeleteSubKey("Freya");
            //關閉 子機碼 路徑
            Reg.Close();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            ShowForm();
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
            }
            // Activate the form.
            this.Activate();
            this.Focus();
            this.ShowInTaskbar = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            radioServer.Stop();
            Close();
            Environment.Exit(Environment.ExitCode);
        }

        private void Freya_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                /*
                notifyIcon1.Tag = string.Empty;
                notifyIcon1.ShowBalloonTip(3000, this.Text,
                     "程式並未結束，要結束請在圖示上按右鍵，選取結束功能!",
                     ToolTipIcon.Info);
                */
                this.ShowInTaskbar = false;
            }

        }

        private void Btn_Options_Click(object sender, EventArgs e)
        {
            FormSetting f = new FormSetting();
            f.radioClient = radioClient;
            f.ShowDialog(this);

            FFunc.GetSettingsFromRegistry(RegSetting);

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

            //** UI Check
            if (FFunc.HasRight(FConstants.FeatureByte.Dark))
            {
                this.button5.Visible = false;
                this.button6.Visible = false;
                this.button8.Visible = false;
            }
            else
            {
                this.button5.Visible = true;
                this.button6.Visible = true;
                this.button8.Visible = true;
            }
        }
    }

    /// 
    /// ----------------------------------------------------------------------------------------------------
    /// 
    //擴充方法 for 跨执行绪更新UI
    public static class Extension
    {
        //非同步委派更新UI
        public static void InvokeIfRequired(
            this Control control, MethodInvoker action)
        {
            if (control.InvokeRequired)//在非當前執行緒內 使用委派
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}