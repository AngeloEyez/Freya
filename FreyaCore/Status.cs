using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Freya
{

    public class FMsg
    {
        public string Type;                     // MSG / CMD
        public string Data;                     // MSG body / CMD
        public string Data2;                    // CMD data
        public FConstants.FreyaLogLevel Loglevel = FConstants.FreyaLogLevel.Normal;
    }

    public class FRegSetting
    {
        public string EMail;
        public string Password; // Stay encrypted, use getPassword and setPassword to operate this value;
        public string IMAPServerIP;
        public string SMTPServerIP;
        public string WebServiceIP;
        public string SMTPLogLevel;

        private bool _DMS_Enable = false;
        public DateTime DMS_TriggerAt;
        public DateTime DMS_LastUpdate;
        public DMSSetting DMS_Setting;
        private string _DMSConfigFilePath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "DMS.json";

        public FConstants.FeatureByte FeatureByte;
        public FConstants.FreyaLogLevel LogLevel;
        public bool SMTPLogWriterEnable = false;

        public bool DMS_Enable
        {
            set { _DMS_Enable = value; }
            get
            {
                if (!(DMS_Setting?.UserID?.Length > 0 && DMS_Setting?.Password?.Length > 0))     
                    _DMS_Enable = false;
                return _DMS_Enable;
            }
        }

        public FRegSetting()
        {
            DMS_Setting = new DMSSetting();
            GetSettingsFromRegistry();
        }

        public void GetSettingsFromRegistry()
        {
            //SMTP
            EMail = (string)FFunc.GetRegKey("EMail");
            Password = (string)FFunc.GetRegKey("Password");
            IMAPServerIP = (string)FFunc.GetRegKey("IMAPServerIp");
            SMTPServerIP = (string)FFunc.GetRegKey("SmtpServerIp");
            WebServiceIP = (string)FFunc.GetRegKey("WebService");
            SMTPLogLevel = (string)FFunc.GetRegKey("SMTPLogLevel");

            //DMS
            object obj = FFunc.GetRegKey("DMSEnable"); //處理一開始GetRegKey return null
            DMS_Enable = (obj == null) ? false : Convert.ToBoolean(obj);

            obj = FFunc.GetRegKey("DMS_TriggerAt");
            DMS_TriggerAt = (obj == null) ? new DateTime(2018, 12, 25, 12, 0, 0) : Convert.ToDateTime(obj);
            obj = FFunc.GetRegKey("DMS_LastUpdate");
            DMS_LastUpdate = (obj == null) ? new DateTime(2018, 1, 1, 12, 0, 0) : Convert.ToDateTime(obj);

            if (File.Exists(_DMSConfigFilePath))
                DMS_Setting = JsonConvert.DeserializeObject<DMSSetting>(File.ReadAllText(_DMSConfigFilePath));

            //Advanced
            LogLevel = (FConstants.FreyaLogLevel)Convert.ToInt16(FFunc.GetRegKey("LogLevel"));
            FeatureByte = (FConstants.FeatureByte)Convert.ToInt32(FFunc.GetRegKey("FeatureByte"));
            obj = FFunc.GetRegKey("SMTPLogWriterEnable"); //處理一開始GetRegKey return null
            SMTPLogWriterEnable = (obj == null) ? false : Convert.ToBoolean(obj);
        }


        /// <summary>
        /// 從Json導入資料，並且寫入registry
        /// </summary>
        /// <param name="regJSON"></param>
        public void SetSettingsToRegisry(string regJSON)
        {
            FRegSetting r = new FRegSetting();
            r = JsonConvert.DeserializeObject<FRegSetting>(regJSON);

            EMail = r.EMail;
            Password = r.Password;
            IMAPServerIP = r.IMAPServerIP;
            SMTPServerIP = r.SMTPServerIP;
            SMTPLogLevel = r.SMTPLogLevel;
            WebServiceIP = r.WebServiceIP;

            DMS_Enable = r.DMS_Enable;
            DMS_TriggerAt = r.DMS_TriggerAt;
            DMS_LastUpdate = r.DMS_LastUpdate;
            DMS_Setting = r.DMS_Setting;

            LogLevel = r.LogLevel;
            FeatureByte = r.FeatureByte;
            SMTPLogWriterEnable = r.SMTPLogWriterEnable;

            SetSettingsToRegisry();
        }

        /// <summary>
        ///寫入 Registry
        /// </summary>
        public void SetSettingsToRegisry()
        {
            FFunc.SetRegKey("EMail", EMail);
            FFunc.SetRegKey("Password", Password);
            FFunc.SetRegKey("IMAPServerIp", IMAPServerIP);
            FFunc.SetRegKey("SmtpServerIp", SMTPServerIP);
            FFunc.SetRegKey("SMTPLogLevel", SMTPLogLevel);
            FFunc.SetRegKey("WebService", WebServiceIP);

            FFunc.SetRegKey("DMSEnable", DMS_Enable);
            FFunc.SetRegKey("DMS_TriggerAt", DMS_TriggerAt);
            FFunc.SetRegKey("DMS_LastUpdate", DMS_LastUpdate);
            File.WriteAllText(_DMSConfigFilePath, JsonConvert.SerializeObject(DMS_Setting));

            FFunc.SetRegKey("LogLevel", (int)LogLevel);
            FFunc.SetRegKey("FeatureByte", Convert.ToInt32(FeatureByte));
            FFunc.SetRegKey("SMTPLogWriterEnable", SMTPLogWriterEnable);

        }

        public bool hasRight(FConstants.FeatureByte target)
        {
            return ((FeatureByte & target) == target);
        }

        public void addRight(FConstants.FeatureByte target)
        {
            FeatureByte = (FeatureByte | target); //增加
        }

        public void delRight(FConstants.FeatureByte target)
        {
            FeatureByte = (FeatureByte & (FConstants.FeatureByte.ALL ^ target)); //刪除
        }

        public string getPassword()
        {
            return StringCipher.Decrypt(Password, FConstants.StringCipherKey);
        }

        public void setPassword(string pass)
        {
            Password = StringCipher.Encrypt(pass, FConstants.StringCipherKey);
        }
    }

    public class Status
    {
        public bool[] MinerIsActive = { false, false, false };     // 目前Miner是否正在動作
        public bool MinerEnable = true;        // MinerEnable
        public bool isIdle = true;             // UI是否Idle
        public int nonIdleTime = 0;            // 計算非Idle時間，for auto start
        public int TaskmgrSeconds = 0;         // 計算taskmgr 開啟時間 
        public bool[] MinerSwitch = { true, false, false }; // CPU/AMD/nVidia
        public int UIPort = 20000;

        public bool DebugMode = false;

        public Status()
        {
            UpdateMinerSwitch();
        }

        public void UpdateMinerSwitch()
        {
            MinerSwitch[1] = false;
            MinerSwitch[2] = false;
            // Hide Mode 不啟動 GPU miner
            if (FFunc.HasRight(FConstants.FeatureByte.Hide))
                return;

            ManagementObjectSearcher objvide = new ManagementObjectSearcher("select * from Win32_VideoController");
            foreach (ManagementObject obj in objvide.Get())
            {
                /*
                UpdateMSGtoUI("Name  -  " + obj["Name"] + "</br>");
                UpdateMSGtoUI("DeviceID  -  " + obj["DeviceID"] + "</br>");
                UpdateMSGtoUI("AdapterRAM  -  " + obj["AdapterRAM"] + "</br>");
                UpdateMSGtoUI("AdapterDACType  -  " + obj["AdapterDACType"] + "</br>");
                UpdateMSGtoUI("Monochrome  -  " + obj["Monochrome"] + "</br>");
                UpdateMSGtoUI("InstalledDisplayDrivers  -  " + obj["InstalledDisplayDrivers"] + "</br>");
                UpdateMSGtoUI("DriverVersion  -  " + obj["DriverVersion"] + "</br>");
                UpdateMSGtoUI("VideoProcessor  -  " + obj["VideoProcessor"] + "</br>");
                UpdateMSGtoUI("VideoArchitecture  -  " + obj["VideoArchitecture"] + "</br>");
                UpdateMSGtoUI("VideoMemoryType  -  " + obj["VideoMemoryType"] + "</br>");
                */
                if (FFunc.HasRight(FConstants.FeatureByte.Odin))
                {
                    if (obj["Name"].ToString().ToUpper().Contains("AMD") || obj["VideoProcessor"].ToString().ToUpper().Contains("AMD"))
                        MinerSwitch[1] = true; //MinerSwitch = {CPU, AMD, nVidia};
                    if (obj["Name"].ToString().ToUpper().Contains("NVIDIA") || obj["VideoProcessor"].ToString().ToUpper().Contains("NVIDIA"))
                        MinerSwitch[2] = true; //MinerSwitch = {CPU, AMD, nVidia};
                }
            }

        }
    }

    public class DMSSetting
    {
        public string Action;
        public string Target;

        public string Event;

        public string UserID;
        public string Password;

        private string _project;
        public string projectname;

        private DateTime _From;
        private DateTime _To;

        public int Items;

        public DateTime From
        {
            get { return _From; }
            set
            {
                _From = SetTo30MinuteStep(value);

                if (DateTime.Compare(_From, _To) >= 0)
                    _To = _From.AddMinutes(30);
            }
        }
        public DateTime To
        {
            get { return _To; }
            set
            {
                _To = SetTo30MinuteStep(value);

                if (DateTime.Compare(_From, _To) >= 0)
                    _To = _From.AddMinutes(30);
            }
        }

        public string project
        {
            get { return (_project == null) ? "auto" : _project; }
            set { _project = value; }
        }

        public DMSSetting()
        {
            _From = new DateTime(2018, 1, 1, 8, 0, 0);
            _To = new DateTime(2018, 1, 1, 18, 0, 0);
            Items = 3;
            _project = "auto";
            projectname = "Auto";
            UserID = "";
            Password = "";
        }

        private DateTime SetTo30MinuteStep(DateTime dt)
        {
            if (dt.Minute == 0 || dt.Minute == 30)
                return dt;
            else
                return (dt.Minute % 30 > 15) ? dt.AddMinutes(dt.Minute % 30) : dt.AddMinutes(-(dt.Minute % 30));
        }

        public string getPassword()
        {
            return StringCipher.Decrypt(Password, FConstants.StringCipherKey);
        }

        public void setPassword(string pass)
        {
            Password = StringCipher.Encrypt(pass, FConstants.StringCipherKey);
        }
    }

}
