using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using ZetaIpc.Runtime.Client;
using Newtonsoft.Json;

namespace Freya.Proxy
{
    /// <summary>
    /// Foundation for IMAP, POP3, and SMTP proxies.
    /// </summary>
    public class ProxyBase
    {
        #region Public Members
        /// <summary>Welcome message to be displayed when connecting.</summary>
        public string WelcomeMessage = "Freya";
        /// <summary>Proxy logging level, determining how much information is logged.</summary>
        public LogLevel LogLevel = LogLevel.None;
        #endregion Public Members

        #region Protected Members
        /// <summary>Whether the proxy has been started.</summary>
        protected bool Started = false;
        /// <summary>A TcpListener to accept incoming connections.</summary>
        protected TcpListener Listener;
        /// <summary>A unique session identifier for logging.</summary>
        protected string SessionId = "";
        /// <summary>A unique connection identifier for logging.</summary>
        protected int ConnectionId = 0;
        /// <summary>StreamWriter object to output event logs and exception information.</summary>
        protected FreyaStreamWriter LogWriter = null, LogWriter2 = null;
        /// <summary>A collection of all S/MIME signing certificates imported during this session.</summary>
        protected X509Certificate2Collection SmimeCertificatesReceived = new X509Certificate2Collection();
        /// <summary>The last command received from the client.</summary>
        protected string LastCommandReceived = "";
        /// <summary>The user transmitting this message.</summary>
        protected string UserName = "";
        #endregion Protected Members

        #region Public Methods
        /// <summary>
        /// Handle service continuations following pauses.
        /// </summary>
        public void ProcessContinuation()
        {
            ProxyFunctions.Log(LogWriter, SessionId, "Service continuing after pause.", Proxy.LogLevel.Information, LogLevel);
            Started = true;
        }

        /// <summary>
        /// Handle pause event.
        /// </summary>
        public void ProcessPause()
        {
            ProxyFunctions.Log(LogWriter, SessionId, "Service pausing.", Proxy.LogLevel.Information, LogLevel);
            Started = false;
        }

        /// <summary>
        /// Handle power events, such as hibernation.
        /// </summary>
        /// <param name="powerStatus">Indicates the system's power status.</param>
        public void ProcessPowerEvent(int powerStatus)
        {
            if (LogWriter != null)
            {
                switch (powerStatus)
                {
                    case 0:
                        ProxyFunctions.Log(LogWriter, SessionId, "Computer has asked permission to enter the suspended state.", Proxy.LogLevel.Information, LogLevel);
                        break;
                    case 2:
                        ProxyFunctions.Log(LogWriter, SessionId, "Computer was denied permission to enter the suspended state.", Proxy.LogLevel.Information, LogLevel);
                        break;
                    case 4:
                        ProxyFunctions.Log(LogWriter, SessionId, "Computer is about to enter the suspended state.", Proxy.LogLevel.Information, LogLevel);
                        break;
                    case 6:
                        ProxyFunctions.Log(LogWriter, SessionId, "Computer has resumed operation after a critical suspension caused by a failing battery.", Proxy.LogLevel.Information, LogLevel);
                        break;
                    case 7:
                        ProxyFunctions.Log(LogWriter, SessionId, "The computer has resumed operation after being suspsended.", Proxy.LogLevel.Information, LogLevel);
                        break;
                    case 8:
                        ProxyFunctions.Log(LogWriter, SessionId, "Computer's battery power is low.", Proxy.LogLevel.Information, LogLevel);
                        break;
                    case 10:
                    case 11:
                        ProxyFunctions.Log(LogWriter, SessionId, "The computer's power status has changed.", Proxy.LogLevel.Information, LogLevel);
                        break;
                    case 18:
                        ProxyFunctions.Log(LogWriter, SessionId, "The computer has resumed operation to handle an event.", Proxy.LogLevel.Information, LogLevel);
                        break;
                }
            }
        }

        /*
        public void SetRadioClientPort(int p)
        {
            LogWriter.SetPort(p);
        }
        */
        #endregion Public Methods
    }

    /// <summary>
    /// 將StreamWriter加上IPC client功能
    /// </summary>
    public class FreyaStreamWriter : StreamWriter
    {
        public IpcClient radioClient = null;
        public bool textLogEn = true;

        public FreyaStreamWriter(string path, bool append, Encoding encoding, int bufferSize, IpcClient radioclient = null) : base (path, append, encoding, bufferSize)
        {
            radioClient = radioclient;
        }

        public string radioSend(string msg, FConstants.FreyaLogLevel loglevel = FConstants.FreyaLogLevel.Normal)
        {
            if (radioClient != null)
                return radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "MSG", Data = msg, Loglevel = loglevel }));
            else
                return "Proxy radioClient is null.";
        }

    }
}