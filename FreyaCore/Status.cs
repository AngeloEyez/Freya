using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freya
{
    public class Status
    {
        public bool MinerIsActive = false;     // 目前Miner是否正在動作
        public bool MinerEnable = true;        // MinerEnable
        public bool isIdle = true;             // UI是否Idle
        public int nonIdleTime = 0;            // 計算非Idle時間，for auto start
        public int TaskmgrSeconds = 0;         // 計算taskmgr 開啟時間 

        public bool DebugMode = false;
    }

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
        public string SMTPServerIP;
        public string WebServiceIP;
        public string SMTPLogLevel;

        public FConstants.FeatureByte FeatureByte;
        public FConstants.FreyaLogLevel LogLevel;
    }
}
