/*
 * OpaqueMail Proxy (https://opaquemail.org/).
 * 
 * Licensed according to the MIT License (http://mit-license.org/).
 * 
 * Copyright © Bert Johnson (https://bertjohnson.com/) of Allcloud Inc. (https://allcloud.com/).
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 */

using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using OpaqueMail;
using ZetaIpc.Runtime.Client;
using MimeKit;
using System.Linq;
using System.Security.Cryptography;
using MailKit;

namespace Freya.Proxy
{
    /// <summary>
    /// IMAP proxy to import S/MIME certificates, filter IPs, or export messages.
    /// </summary>
    public class ImapProxy : ProxyBase
    {
        private bool _enable = false;
        public bool proxyrunning { get; private set; } = false;

        #region Public Methods
        /// <summary>
        /// Start a IMAP proxy instance.
        /// </summary>
        /// <param name="acceptedIPs">IP addresses to accept connections from.</param>
        /// <param name="localIPAddress">Local IP address to bind to.</param>
        /// <param name="localPort">Local port to listen on.</param>
        /// <param name="localEnableSsl">Whether the local server supports TLS/SSL.</param>
        /// <param name="remoteServerHostName">Remote server hostname to forward all IMAP messages to.</param>
        /// <param name="remoteServerPort">Remote server port to connect to.</param>
        /// <param name="remoteServerEnableSsl">Whether the remote IMAP server requires TLS/SSL.</param>
        public void Start(string acceptedIPs, IPAddress localIPAddress, int localPort, bool localEnableSsl, string remoteServerHostName, int remoteServerPort, bool remoteServerEnableSsl)
        {
            Start(acceptedIPs, localIPAddress, localPort, localEnableSsl, remoteServerHostName, remoteServerPort, remoteServerEnableSsl, null, "", "", LogLevel.None, 0, false);
        }

        /// <summary>
        /// Start a IMAP proxy instance.
        /// </summary>
        /// <param name="acceptedIPs">IP addresses to accept connections from.</param>
        /// <param name="localIPAddress">Local IP address to bind to.</param>
        /// <param name="localPort">Local port to listen on.</param>
        /// <param name="localEnableSsl">Whether the local server supports TLS/SSL.</param>
        /// <param name="remoteServerHostName">Remote server hostname to forward all IMAP messages to.</param>
        /// <param name="remoteServerPort">Remote server port to connect to.</param>
        /// <param name="remoteServerEnableSsl">Whether the remote IMAP server requires TLS/SSL.</param>
        /// <param name="remoteServerCredential">(Optional) Credentials to be used for all connections to the remote IMAP server.  When set, this overrides any credentials passed locally.</param>
        /// <param name="exportDirectory">(Optional) Location where all incoming messages are saved as EML files.</param>
        /// <param name="logFile">File where event logs and exception information will be written.</param>
        /// <param name="logLevel">Proxy logging level, determining how much information is logged.</param>
        /// <param name="instanceId">The instance number of the proxy.</param>
        /// <param name="debugMode">Whether the proxy instance is running in DEBUG mode and should output full exception messages.</param>
        public void Start(string acceptedIPs, IPAddress localIPAddress, int localPort, bool localEnableSsl, string remoteServerHostName, int remoteServerPort, bool remoteServerEnableSsl, NetworkCredential remoteServerCredential, string exportDirectory, string logFile, LogLevel logLevel, int instanceId, bool debugMode,
            bool LogWriterEnable = false,
            IpcClient radioClient = null)
        {
            // Create the log writer.
            string logFileName = "";
            if (!string.IsNullOrEmpty(logFile))
            {
                logFileName = ProxyFunctions.GetLogFileName(logFile, instanceId, localIPAddress.ToString(), remoteServerHostName, localPort, remoteServerPort);
                LogWriter = new FreyaStreamWriter(logFileName, true, Encoding.UTF8, Constants.SMALLBUFFERSIZE, radioClient);
                LogWriter.textLogEn = LogWriterEnable;
                LogWriter.AutoFlush = true;
                LogLevel = logLevel;

                LogWriter2 = new FreyaStreamWriter(logFileName + "2", true, Encoding.UTF8, Constants.SMALLBUFFERSIZE, radioClient);
                LogWriter2.textLogEn = LogWriterEnable;
                LogWriter2.AutoFlush = true;
            }

            // Make sure the remote server isn't an infinite loop back to this server.
            string fqdn = Functions.GetLocalFQDN();
            if (remoteServerHostName.ToUpper() == fqdn.ToUpper() && remoteServerPort == localPort)
            {
                ProxyFunctions.Log(LogWriter, SessionId, "Cannot start IMAP service because the remote server host name {" + remoteServerHostName + "} and port {" + remoteServerPort.ToString() + "} is the same as this proxy, which would cause an infinite loop.", Proxy.LogLevel.Critical, LogLevel);
                return;
            }
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress hostIP in hostEntry.AddressList)
            {
                if (remoteServerHostName == hostIP.ToString() && remoteServerPort == localPort)
                {
                    ProxyFunctions.Log(LogWriter, SessionId, "Cannot start IMAP service because the remote server hostname {" + remoteServerHostName + "} and port {" + remoteServerPort.ToString() + "} is the same as this proxy, which would cause an infinite loop.", Proxy.LogLevel.Critical, LogLevel);
                    return;
                }
            }

            ProxyFunctions.Log(LogWriter, SessionId, "IMAP Starting service.", Proxy.LogLevel.Information, LogLevel);

            // Attempt to start up to 3 times in case another service using the port is shutting down.
            int startAttempts = 0;
            _enable = true;
            while (_enable && startAttempts < 3)
            {
                proxyrunning = true;
                startAttempts++;

                // If we've failed to start once, wait an extra 10 seconds.
                if (startAttempts > 1)
                {
                    ProxyFunctions.Log(LogWriter, SessionId, "IMAP Attempting to start for the " + (startAttempts == 2 ? "2nd" : "3rd") + " time.", Proxy.LogLevel.Information, LogLevel);
                    Thread.Sleep(10000 * startAttempts);
                }

                try
                {
                    X509Certificate serverCertificate = null;

                    // Generate a unique session ID for logging.
                    SessionId = Guid.NewGuid().ToString();
                    ConnectionId = 0;

                    // If local SSL is supported via STARTTLS, ensure we have a valid server certificate.
                    if (localEnableSsl)
                    {
                        // Load the SSL certificate for the listening server name.
                        serverCertificate = CertHelper.GetCertificateBySubjectName(StoreLocation.LocalMachine, fqdn);

                        // In case the service as running as the current user, check the Current User certificate store as well.
                        if (serverCertificate == null)
                            serverCertificate = CertHelper.GetCertificateBySubjectName(StoreLocation.CurrentUser, fqdn);

                        // If no certificate was found, generate a self-signed certificate.
                        if (serverCertificate == null)
                        {
                            ProxyFunctions.Log(LogWriter, SessionId, "No signing certificate found, so generating new certificate.", Proxy.LogLevel.Warning, LogLevel);

                            List<DerObjectIdentifier> oids = new List<DerObjectIdentifier>();
                            oids.Add(new DerObjectIdentifier("1.3.6.1.5.5.7.3.1"));    // Server Authentication.

                            // Generate the certificate with a duration of 10 years, 4096-bits, and a key usage of server authentication.
                            serverCertificate = CertHelper.CreateSelfSignedCertificate(fqdn, fqdn, StoreLocation.LocalMachine, true, 4096, 10, oids);

                            ProxyFunctions.Log(LogWriter, SessionId, "Certificate generated with Serial Number {" + serverCertificate.GetSerialNumberString() + "}.", Proxy.LogLevel.Information, LogLevel);
                        }
                    }

                    Listener = new TcpListener(localIPAddress, localPort);
                    Listener.Start();

                    ProxyFunctions.Log(LogWriter, SessionId, "IMAP Service started.", Proxy.LogLevel.Information, LogLevel);
                    ProxyFunctions.Log(LogWriter, SessionId, "IMAP Service Listening on address {" + localIPAddress.ToString() + "}, port {" + localPort + "}.", Proxy.LogLevel.Information, LogLevel);

                    Started = true;

                    // Accept client requests, forking each into its own thread.
                    while (Started)
                    {
                        TcpClient client = Listener.AcceptTcpClient();

                        string newLogFileName = ProxyFunctions.GetLogFileName(logFile, instanceId, localIPAddress.ToString(), remoteServerHostName, localPort, remoteServerPort);
                        if (newLogFileName != logFileName)
                        {
                            if (LogWriter != null)
                                LogWriter.Close();
                            LogWriter = new FreyaStreamWriter(newLogFileName, true, Encoding.UTF8, Constants.SMALLBUFFERSIZE, radioClient);
                            LogWriter.AutoFlush = true;
                            LogWriter.textLogEn = LogWriterEnable;
                        }

                        try
                        {
                            // Prepare the arguments for our new thread.
                            ImapProxyConnectionArguments arguments = new ImapProxyConnectionArguments();
                            arguments.AcceptedIPs = acceptedIPs;
                            arguments.TcpClient = client;
                            arguments.Certificate = serverCertificate;
                            arguments.ExportDirectory = exportDirectory;
                            arguments.LocalIpAddress = localIPAddress;
                            arguments.LocalPort = localPort;
                            arguments.LocalEnableSsl = localEnableSsl;
                            arguments.RemoteServerHostName = remoteServerHostName;
                            arguments.RemoteServerPort = remoteServerPort;
                            arguments.RemoteServerEnableSsl = remoteServerEnableSsl;
                            arguments.RemoteServerCredential = remoteServerCredential;

                            // Increment the connection counter;
                            arguments.ConnectionId = (unchecked(++ConnectionId)).ToString();
                            arguments.InstanceId = instanceId;
                            arguments.DebugMode = debugMode;

                            // Fork the thread and continue listening for new connections.
                            Task.Run(() => ProcessConnection(arguments));
                        }
                        catch (Exception ex)
                        {
                            ProxyFunctions.Log(LogWriter, SessionId, "Error while IMAP processing connection: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    if (debugMode || System.Diagnostics.Debugger.IsAttached)
                        ProxyFunctions.Log(LogWriter, SessionId, "Exception when starting IMAP proxy: " + ex.ToString(), Proxy.LogLevel.Critical, LogLevel);
                    else
                        ProxyFunctions.Log(LogWriter, SessionId, "Exception when starting IMAP proxy: " + ex.Message, Proxy.LogLevel.Critical, LogLevel);
                }
            }
            proxyrunning = false;
        }

        /// <summary>
        /// Stop the IMAP proxy and close all existing connections.
        /// </summary>
        public void Stop()
        {
            ProxyFunctions.Log(LogWriter, SessionId, "IMAP Stopping service.", Proxy.LogLevel.Information, LogLevel);

            while (proxyrunning)
            {
                _enable = false;
                Started = false;

                if (Listener != null)
                    Listener.Stop();
            }

            ProxyFunctions.Log(LogWriter, SessionId, "IMAP Service stopped.", Proxy.LogLevel.Information, LogLevel);
        }

        /// <summary>
        /// Start all IMAP proxy instances from the specified settings file.
        /// </summary>
        /// <param name="fileName">File containing the IMAP proxy settings.</param>
        public static List<ImapProxy> StartProxiesFromSettingsFile(string fileName)
        {
            List<ImapProxy> imapProxies = new List<ImapProxy>();

            try
            {
                if (File.Exists(fileName))
                {
                    XPathDocument document = new XPathDocument(fileName);
                    XPathNavigator navigator = document.CreateNavigator();

                    int imapServiceCount = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/IMAP/ServiceCount");
                    for (int i = 1; i <= imapServiceCount; i++)
                    {
                        ImapProxyArguments arguments = new ImapProxyArguments();
                        arguments.AcceptedIPs = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/AcceptedIPs");

                        string localIpAddress = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/IMAP/Service" + i + "/LocalIPAddress").ToUpper();
                        switch (localIpAddress)
                        {
                            // Treat blank values as "Any".
                            case "":
                            case "ANY":
                                arguments.LocalIpAddress = IPAddress.Any;
                                break;
                            case "BROADCAST":
                                arguments.LocalIpAddress = IPAddress.Broadcast;
                                break;
                            case "IPV6ANY":
                                arguments.LocalIpAddress = IPAddress.IPv6Any;
                                break;
                            case "IPV6LOOPBACK":
                                arguments.LocalIpAddress = IPAddress.IPv6Loopback;
                                break;
                            case "LOOPBACK":
                                arguments.LocalIpAddress = IPAddress.Loopback;
                                break;
                            default:
                                // Try to parse the local IP address.  If unable to, proceed to the next service instance.
                                if (!IPAddress.TryParse(localIpAddress, out arguments.LocalIpAddress))
                                    continue;
                                break;
                        }

                        arguments.LocalPort = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/IMAP/Service" + i + "/LocalPort");
                        // If the port is invalid, proceed to the next service instance.
                        if (arguments.LocalPort < 1)
                            continue;

                        arguments.LocalEnableSsl = ProxyFunctions.GetXmlBoolValue(navigator, "/Settings/IMAP/Service" + i + "/LocalEnableSSL");

                        arguments.RemoteServerHostName = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/IMAP/Service" + i + "/RemoteServerHostName");
                        // If the host name is invalid, proceed to the next service instance.
                        if (string.IsNullOrEmpty(arguments.RemoteServerHostName))
                            continue;

                        arguments.RemoteServerPort = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/IMAP/Service" + i + "/RemoteServerPort");
                        // If the port is invalid, proceed to the next service instance.
                        if (arguments.RemoteServerPort < 1)
                            continue;

                        arguments.RemoteServerEnableSsl = ProxyFunctions.GetXmlBoolValue(navigator, "/Settings/IMAP/Service" + i + "/RemoteServerEnableSSL");

                        string remoteServerUsername = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/IMAP/Service" + i + "/RemoteServerUsername");
                        if (!string.IsNullOrEmpty(remoteServerUsername))
                        {
                            arguments.RemoteServerCredential = new NetworkCredential();
                            arguments.RemoteServerCredential.UserName = remoteServerUsername;
                            arguments.RemoteServerCredential.Password = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/IMAP/Service" + i + "/RemoteServerPassword");
                        }

                        string certificateLocationValue = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/IMAP/Service" + i + "/Certificate/Location");
                        StoreLocation certificateLocation = StoreLocation.LocalMachine;
                        if (certificateLocationValue.ToUpper() == "CURRENTUSER")
                            certificateLocation = StoreLocation.CurrentUser;

                        // Try to load the signing certificate based on its serial number first, then fallback to its subject name.
                        string certificateValue = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/IMAP/Service" + i + "/Certificate/SerialNumber");
                        if (!string.IsNullOrEmpty(certificateValue))
                            arguments.Certificate = CertHelper.GetCertificateBySerialNumber(certificateLocation, certificateValue);
                        else
                        {
                            certificateValue = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/IMAP/Service" + i + "/Certificate/SubjectName");
                            if (!string.IsNullOrEmpty(certificateValue))
                                arguments.Certificate = CertHelper.GetCertificateBySubjectName(certificateLocation, certificateValue);
                        }

                        arguments.ExportDirectory = ProxyFunctions.GetXmlStringValue(navigator, "Settings/IMAP/Service" + i + "/ExportDirectory");
                        arguments.LogFile = ProxyFunctions.GetXmlStringValue(navigator, "Settings/IMAP/Service" + i + "/LogFile");

                        string logLevel = ProxyFunctions.GetXmlStringValue(navigator, "Settings/IMAP/Service" + i + "/LogLevel");
                        switch (logLevel.ToUpper())
                        {
                            case "NONE":
                                arguments.LogLevel = LogLevel.None;
                                break;
                            case "CRITICAL":
                                arguments.LogLevel = LogLevel.Critical;
                                break;
                            case "ERROR":
                                arguments.LogLevel = LogLevel.Error;
                                break;
                            case "RAW":
                                arguments.LogLevel = LogLevel.Raw;
                                break;
                            case "VERBOSE":
                                arguments.LogLevel = LogLevel.Verbose;
                                break;
                            case "WARNING":
                                arguments.LogLevel = LogLevel.Warning;
                                break;
                            case "INFORMATION":
                            default:
                                arguments.LogLevel = LogLevel.Information;
                                break;
                        }

                        arguments.InstanceId = i;
                        arguments.DebugMode = ProxyFunctions.GetXmlBoolValue(navigator, "Settings/IMAP/Service" + i + "/Debug");

                        // Remember the proxy in order to close it when the service stops.
                        arguments.Proxy = new ImapProxy();
                        imapProxies.Add(arguments.Proxy);

                        Thread proxyThread = new Thread(new ParameterizedThreadStart(StartProxy));
                        proxyThread.Name = "OpaqueMail IMAP Proxy";
                        proxyThread.Start(arguments);
                    }
                }
            }
            catch
            {
                // Ignore errors if the XML settings file is malformed.
            }

            return imapProxies;
        }

        public static List<ImapProxy> StartProxiesFromFromRegistry(FRegSetting reg, IpcClient radioClient = null)
        {
            List<ImapProxy> imapProxies = new List<ImapProxy>();

            try
            {

                //int imapServiceCount = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/IMAP/ServiceCount");
                int imapServiceCount = 1; //暫時僅支援1個proxy
                for (int i = 1; i <= imapServiceCount; i++)
                {
                    ImapProxyArguments arguments = new ImapProxyArguments();
                    arguments.AcceptedIPs = "127.0.0.1";

                    string localIpAddress = "ANY";
                    switch (localIpAddress)
                    {
                        // Treat blank values as "Any".
                        case "":
                        case "ANY":
                            arguments.LocalIpAddress = IPAddress.Any;
                            break;
                        case "BROADCAST":
                            arguments.LocalIpAddress = IPAddress.Broadcast;
                            break;
                        case "IPV6ANY":
                            arguments.LocalIpAddress = IPAddress.IPv6Any;
                            break;
                        case "IPV6LOOPBACK":
                            arguments.LocalIpAddress = IPAddress.IPv6Loopback;
                            break;
                        case "LOOPBACK":
                            arguments.LocalIpAddress = IPAddress.Loopback;
                            break;
                        default:
                            // Try to parse the local IP address.  If unable to, proceed to the next service instance.
                            if (!IPAddress.TryParse(localIpAddress, out arguments.LocalIpAddress))
                                continue;
                            break;
                    }

                    arguments.LocalPort = 143;
                    // If the port is invalid, proceed to the next service instance.
                    if (arguments.LocalPort < 1)
                        continue;

                    arguments.LocalEnableSsl = false;

                    arguments.RemoteServerHostName = reg.IMAPServerIP;
                    // If the host name is invalid, proceed to the next service instance.
                    if (string.IsNullOrEmpty(arguments.RemoteServerHostName))
                        continue;

                    arguments.RemoteServerPort = 993;
                    // If the port is invalid, proceed to the next service instance.
                    if (arguments.RemoteServerPort < 1)
                        continue;

                    arguments.RemoteServerEnableSsl = true;

                    string remoteServerUsername = reg.EMail;
                    if (!string.IsNullOrEmpty(remoteServerUsername))
                    {
                        arguments.RemoteServerCredential = new NetworkCredential();
                        arguments.RemoteServerCredential.UserName = remoteServerUsername;
                        arguments.RemoteServerCredential.Password = reg.getPassword();
                    }

                    string certificateLocationValue = "LOCALMACHINE";
                    StoreLocation certificateLocation = StoreLocation.LocalMachine;
                    if (certificateLocationValue.ToUpper() == "CURRENTUSER")
                        certificateLocation = StoreLocation.CurrentUser;

                    // Try to load the signing certificate based on its serial number first, then fallback to its subject name.
                    string certificateValue = "";
                    if (!string.IsNullOrEmpty(certificateValue))
                        arguments.Certificate = CertHelper.GetCertificateBySerialNumber(certificateLocation, certificateValue);
                    else
                    {
                        certificateValue = "";
                        if (!string.IsNullOrEmpty(certificateValue))
                            arguments.Certificate = CertHelper.GetCertificateBySubjectName(certificateLocation, certificateValue);
                    }

                    arguments.ExportDirectory = "";
                    arguments.LogFile = @"Logs\IMAPProxy{#}-{yyyy-MM-dd}.log";

                    string logLevel = reg.SMTPLogLevel;
                    switch (logLevel.ToUpper())
                    {
                        case "NONE":
                            arguments.LogLevel = LogLevel.None;
                            break;
                        case "CRITICAL":
                            arguments.LogLevel = LogLevel.Critical;
                            break;
                        case "ERROR":
                            arguments.LogLevel = LogLevel.Error;
                            break;
                        case "RAW":
                            arguments.LogLevel = LogLevel.Raw;
                            break;
                        case "VERBOSE":
                            arguments.LogLevel = LogLevel.Verbose;
                            break;
                        case "WARNING":
                            arguments.LogLevel = LogLevel.Warning;
                            break;
                        case "INFORMATION":
                        default:
                            arguments.LogLevel = LogLevel.Information;
                            break;
                    }

                    arguments.InstanceId = i;
                    arguments.DebugMode = false;

                    arguments.LogWriteEnable = reg.SMTPLogWriterEnable;
                    arguments.radioClient = radioClient;

                    // Remember the proxy in order to close it when the service stops.
                    arguments.Proxy = new ImapProxy();
                    imapProxies.Add(arguments.Proxy);

                    Thread proxyThread = new Thread(new ParameterizedThreadStart(StartProxy));
                    proxyThread.Name = "Freya IMAP Proxy";
                    proxyThread.Start(arguments);
                }

            }
            catch
            {
                // Ignore errors if the XML settings file is malformed.
            }

            return imapProxies;
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Handle an incoming IMAP connection, from connection to completion.
        /// </summary>
        /// <param name="parameters">ImapProxyConnectionArguments object containing all parameters for this connection.</param>
        private void ProcessConnection(object parameters)
        {
            // Cast the passed-in parameters back to their original objects.
            ImapProxyConnectionArguments arguments = (ImapProxyConnectionArguments)parameters;

            try
            {
                TcpClient client = arguments.TcpClient;
                Stream clientStream = client.GetStream();

                // Capture the client's IP information.
                PropertyInfo pi = clientStream.GetType().GetProperty("Socket", BindingFlags.NonPublic | BindingFlags.Instance);
                string ip = ((Socket)pi.GetValue(clientStream, null)).RemoteEndPoint.ToString();
                if (ip.IndexOf(":") > -1)
                    ip = ip.Substring(0, ip.IndexOf(":"));

                // If the IP address range filter contains the localhost entry 0.0.0.0, check if the client IP is a local address and update it to 0.0.0.0 if so.
                if (arguments.AcceptedIPs.IndexOf("0.0.0.0") > -1)
                {
                    if (ip == "127.0.0.1")
                        ip = "0.0.0.0";
                    else
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                        foreach (IPAddress hostIP in hostEntry.AddressList)
                        {
                            if (hostIP.ToString() == ip)
                            {
                                ip = "0.0.0.0";
                                break;
                            }
                        }
                    }
                }

                // Validate that the IP address is within an accepted range.
                if (!ProxyFunctions.ValidateIP(arguments.AcceptedIPs, ip))
                {
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "IMAP Connection rejected from {" + ip + "} due to its IP address.", Proxy.LogLevel.Warning, LogLevel);

                    Functions.SendStreamString(clientStream, new byte[Constants.SMALLBUFFERSIZE], "500 IP address [" + ip + "] rejected.\r\n");

                    if (clientStream != null)
                        clientStream.Dispose();
                    if (client != null)
                        client.Close();

                    return;
                }

                //ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, $"IMAP connection {arguments.ConnectionId} established from {ip}.", Proxy.LogLevel.Information, LogLevel);

                // If supported, upgrade the session's security through a TLS handshake.
                if (arguments.LocalEnableSsl)
                {
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, $"IMAP Starting local TLS/SSL protection form {ip}.", Proxy.LogLevel.Information, LogLevel);
                    clientStream = new SslStream(clientStream);
                    ((SslStream)clientStream).AuthenticateAsServer(arguments.Certificate);
                }
                else
                {
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, $"IMAP connection established from {ip}.", Proxy.LogLevel.Information, LogLevel);
                }


                // Connect to the remote server.
                TcpClient remoteServerClient = new TcpClient(arguments.RemoteServerHostName, arguments.RemoteServerPort);
                Stream remoteServerStream = remoteServerClient.GetStream();

                // If supported, upgrade the session's security through a TLS handshake.
                if (arguments.RemoteServerEnableSsl)
                {
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, $"IMAP Starting remote TLS/SSL protection with {arguments.RemoteServerHostName}.", Proxy.LogLevel.Information, LogLevel);
                    //remoteServerStream = new SslStream(remoteServerStream);
                    //Modify for Certification Error call back
                    remoteServerStream = new SslStream(remoteServerStream, false, new RemoteCertificateValidationCallback((s, c, h, e) => { return true; }));

                    ((SslStream)remoteServerStream).AuthenticateAsClient(arguments.RemoteServerHostName);
                }

                // Relay server data to the client.
                TransmitArguments remoteServerToClientArguments = new TransmitArguments();
                remoteServerToClientArguments.ClientStream = remoteServerStream;
                remoteServerToClientArguments.RemoteServerStream = clientStream;
                remoteServerToClientArguments.IsClient = false;
                remoteServerToClientArguments.ConnectionId = ConnectionId.ToString();
                remoteServerToClientArguments.InstanceId = arguments.InstanceId;
                remoteServerToClientArguments.DebugMode = arguments.DebugMode;
                remoteServerToClientArguments.IPAddress = ip;
                remoteServerToClientArguments.ExportDirectory = arguments.ExportDirectory;
                Task.Run(() => RelayData(remoteServerToClientArguments));

                // Relay client data to the remote server.
                TransmitArguments clientToRemoteServerArguments = new TransmitArguments();
                clientToRemoteServerArguments.ClientStream = clientStream;
                clientToRemoteServerArguments.RemoteServerStream = remoteServerStream;
                clientToRemoteServerArguments.IsClient = true;
                clientToRemoteServerArguments.ConnectionId = ConnectionId.ToString();
                clientToRemoteServerArguments.InstanceId = arguments.InstanceId;
                clientToRemoteServerArguments.DebugMode = arguments.DebugMode;
                clientToRemoteServerArguments.IPAddress = ip;
                clientToRemoteServerArguments.Credential = arguments.RemoteServerCredential;
                Task.Run(() => RelayData(clientToRemoteServerArguments));
            }
            catch (SocketException ex)
            {
                if (arguments.DebugMode || System.Diagnostics.Debugger.IsAttached)
                    ProxyFunctions.Log(LogWriter, SessionId, "Exception communicating with {" + arguments.RemoteServerHostName + "} on port {" + arguments.RemoteServerPort + "}: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                else
                    ProxyFunctions.Log(LogWriter, SessionId, "Exception communicating with {" + arguments.RemoteServerHostName + "} on port {" + arguments.RemoteServerPort + "}: " + ex.Message, Proxy.LogLevel.Error, LogLevel);
            }
            catch (Exception ex)
            {
                if (arguments.DebugMode || System.Diagnostics.Debugger.IsAttached)
                    ProxyFunctions.Log(LogWriter, SessionId, "Exception: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                else
                    ProxyFunctions.Log(LogWriter, SessionId, "Exception: " + ex.Message, Proxy.LogLevel.Error, LogLevel);
            }
        }

        /// <summary>
        /// Relay data read from one connection to another.
        /// </summary>
        /// <param name="o">A TransmitArguments object containing local and remote server parameters.</param>
        private async void RelayData(object o)
        {
            // Cast the passed-in parameters back to their original objects.
            TransmitArguments arguments = (TransmitArguments)o;
            Stream clientStream = arguments.ClientStream;
            Stream remoteServerStream = arguments.RemoteServerStream;

            // A byte array to streamline bit shuffling.
            char[] buffer = new char[Constants.SMALLBUFFERSIZE];

            // Placeholder variables to track the current message being transmitted.
            bool inMessage = false;
            int messageLength = 0;
            StringBuilder messageBuilder = new StringBuilder(Constants.SMALLSBSIZE);

            // The overall number of bytes transmitted on this connection.
            ulong bytesTransmitted = 0;

            //  When a "[THROTTLED]" notice was last received.
            DateTime lastThrottleTime = new DateTime(1900, 1, 1);

            if (arguments.Credential != null)
                UserName = arguments.Credential.UserName;

            // MsgMod:
            string cmdBuf = "";   // 開始傳Message的這條cmd buffer
            bool ignore = false;

            bool stillReceiving = true;
            try
            {
                using (StreamReader clientStreamReader = new StreamReader(clientStream))
                {
                    using (StreamWriter remoteServerStreamWriter = new StreamWriter(remoteServerStream))
                    {
                        remoteServerStreamWriter.AutoFlush = true;

                        while (Started && stillReceiving)
                        {
                            // Read data from the source and send it to its destination.
                            string stringRead = await clientStreamReader.ReadLineAsync();

                            if (stringRead != null)
                            {
                                int bytesRead = stringRead.Length;
                                bytesTransmitted += (ulong)bytesRead;

                                // If this data comes from the client, log it.  Otherwise, process it.
                                if (arguments.IsClient)
                                {
                                    bool messageRelayed = false;

                                    string[] commandParts = stringRead.Split(new char[] { ' ' }, 4);
                                    if (commandParts.Length > 1)
                                    {
                                        // Optionally, transform the login details.
                                        if (commandParts[1] == "LOGIN" && commandParts.Length == 4)
                                            messageRelayed = TransformLogin(remoteServerStreamWriter, stringRead, arguments, ref UserName);
                                        else
                                        {
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId.ToString(), "C: " + stringRead, Proxy.LogLevel.Raw, LogLevel);
                                        }

                                        // Remember the previous command.
                                        if (commandParts[1].ToUpper() == "UID" && commandParts.Length > 2)
                                            LastCommandReceived = (commandParts[1] + " " + commandParts[2]).ToUpper();
                                        else
                                            LastCommandReceived = commandParts[1].ToUpper();

                                        // Stop after a logout order is received.
                                        if (LastCommandReceived == "LOGOUT")
                                            stillReceiving = false;

                                        if (LogLevel == Proxy.LogLevel.Verbose)
                                        {
                                            switch (LastCommandReceived)
                                            {
                                                case "APPEND":
                                                case "AUTHENTICATE":
                                                case "CAPABILITY":
                                                case "CHECK":
                                                case "CLOSE":
                                                case "COPY":
                                                case "CREATE":
                                                case "DELETE":
                                                case "ENABLE":
                                                case "EXAMINE":
                                                case "EXPUNGE":
                                                case "FETCH":
                                                case "GETQUOTA":
                                                case "GETQUOTAROOT":
                                                case "LIST":
                                                case "LOGIN":
                                                case "LOGOUT":
                                                case "LSUB":
                                                case "MOVE":
                                                case "NOOP":
                                                case "NOTIFY":
                                                case "RENAME":
                                                case "SEARCH":
                                                case "SELECT":
                                                case "SETQUOTA":
                                                case "STATUS":
                                                case "STORE":
                                                case "SUBSCRIBE":
                                                case "UID COPY":
                                                case "UID FETCH":
                                                case "UID SEARCH":
                                                case "UID STORE":
                                                case "UNSUBSCRIBE":
                                                case "XLIST":
                                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId.ToString(), "Command {" + LastCommandReceived + "} processed.", Proxy.LogLevel.Verbose, LogLevel);
                                                    break;
                                            }
                                        }
                                    }

                                    // If the command wasn't processed, send the raw command.
                                    if (!messageRelayed)
                                        await remoteServerStreamWriter.WriteLineAsync(stringRead);
                                }
                                else
                                {
                                    // If we're currently receiving a message, check to see if it's completed.
                                    if (inMessage)
                                    {
                                        messageBuilder.AppendLine(stringRead);

                                        // 系統發出 spamlist長度有問題，看到)來判斷message結束
                                        if (messageBuilder.Length >= messageLength || stringRead.Equals(")"))
                                        {
                                            //去掉最後一個NewLine，stringbuilder轉出來會自己多加一個，造成outlook無法解析郵件
                                            string messageBuilderStr = messageBuilder.ToString();
                                            messageBuilderStr = messageBuilderStr.Substring(0, messageBuilderStr.LastIndexOf(Environment.NewLine));
                                            int lengtgDiff = messageBuilder.Length - messageBuilderStr.Length;

                                            //Build message
                                            string message, endstr = string.Empty;
                                            if (messageBuilder.Length < messageLength)
                                            {
                                                message = messageBuilder.ToString(0, messageBuilder.ToString().LastIndexOf(")"));
                                            }
                                            else
                                                message = messageBuilderStr.Substring(0, messageLength - lengtgDiff);

                                            // If the message has been completed and it contains a signature, process it.
                                            if (message.IndexOf("application/x-pkcs7-signature") > -1 || message.IndexOf("application/pkcs7-mime") > -1 || !string.IsNullOrEmpty(arguments.ExportDirectory))
                                            {
                                                Thread processThread = new Thread(new ParameterizedThreadStart(ProcessMessage));
                                                processThread.Name = "OpaqueMail IMAP Proxy Signature Processor";
                                                ProcessMessageArguments processMessageArguments = new ProcessMessageArguments();
                                                processMessageArguments.MessageText = message;
                                                processMessageArguments.ConnectionId = ConnectionId.ToString();
                                                processMessageArguments.ExportDirectory = arguments.ExportDirectory;
                                                processMessageArguments.InstanceId = arguments.InstanceId;
                                                processMessageArguments.DebugMode = arguments.DebugMode;
                                                processMessageArguments.UserName = UserName;
                                                processThread.Start(processMessageArguments);
                                            }


                                            //messageBuilder扣除message多讀取的資料
                                            string overRead = string.Empty;
                                            if (messageBuilderStr.Length > message.Length)
                                                overRead = messageBuilderStr.Substring(message.Length, messageBuilderStr.Length - message.Length); //.TrimEnd(Environment.NewLine.ToCharArray());

                                            //處理Message
                                            string newMessage = DecryptMessage(message);

                                            if (!message.Equals(newMessage))
                                                cmdBuf = cmdBuf.Replace(messageLength.ToString(), (newMessage.Length + lengtgDiff).ToString());

                                            await remoteServerStreamWriter.WriteLineAsync(cmdBuf);
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId.ToString(), "#S: " + cmdBuf, Proxy.LogLevel.Raw, LogLevel);

                                            /*
                                            // 分行送出，跟一次送出看起來沒差別
                                            foreach (var myString in newMessage.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                                            {
                                                await remoteServerStreamWriter.WriteLineAsync(myString);
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId.ToString(), string.Format("#S: {0}", myString), Proxy.LogLevel.Raw, LogLevel);
                                            }
                                            */
                                            await remoteServerStreamWriter.WriteLineAsync(newMessage + overRead);
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId.ToString(), string.Format("#S: {0}\n\n.\n.\n.\n\n{1}", newMessage.Substring(0, 200), newMessage.Substring(newMessage.Length - 200, 200)), Proxy.LogLevel.Raw, LogLevel);

                                            // We're no longer receiving a message, so continue.
                                            inMessage = false;
                                            ignore = true;
                                            messageBuilder.Clear();
                                        }
                                    }
                                    else
                                    {
                                        // Disallow proxy stream compression.
                                        if (stringRead.StartsWith("* CAPABILITY"))
                                            stringRead = stringRead.Replace(" COMPRESS=DEFLATE", "");

                                        // Check for IMAP meta flags.
                                        string betweenBraces = Functions.ReturnBetween(stringRead, "[", "]");
                                        switch (betweenBraces)
                                        {
                                            case "CLIENTBUG":
                                            case "CORRUPTION":
                                            case "OVERQUOTA":
                                            case "SERVERBUG":
                                            case "UNAVAILABLE":
                                                ProxyFunctions.Log(LogWriter, SessionId, ConnectionId.ToString(), stringRead.Substring(stringRead.IndexOf("[")), Proxy.LogLevel.Warning, LogLevel);
                                                break;
                                            case "THROTTLED":
                                                if (DateTime.Now - lastThrottleTime >= new TimeSpan(0, 20, 0))
                                                {
                                                    ProxyFunctions.Log(LogWriter, SessionId, ConnectionId.ToString(), "Connection speed throttled by the remote server.", Proxy.LogLevel.Warning, LogLevel);
                                                    lastThrottleTime = DateTime.Now;
                                                }
                                                break;
                                        }

                                        // Messages are denoted by FETCH headers with their lengths in curly braces.
                                        if (stringRead.ToUpper().Contains(" FETCH "))
                                        {
                                            int openBrace = stringRead.IndexOf("{");
                                            if (openBrace > -1)
                                            {
                                                int closeBrace = stringRead.IndexOf("}", openBrace);
                                                if (closeBrace > -1)
                                                {
                                                    // Only proceed if we can parse the size of the message.
                                                    if (int.TryParse(stringRead.Substring(openBrace + 1, closeBrace - openBrace - 1), out messageLength))
                                                    {
                                                        inMessage = true;
                                                        cmdBuf = stringRead; // 記下這條CMD, 並hold住remoteServerStreamWriter
                                                    }
                                                }
                                            }
                                        }


                                    }

                                    if (!inMessage)
                                    {
                                        if (!ignore)
                                        {
                                            await remoteServerStreamWriter.WriteLineAsync(stringRead);
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId.ToString(), "S: " + stringRead, Proxy.LogLevel.Raw, LogLevel);
                                        }
                                        else
                                            ignore = false;
                                    }

                                }
                            }
                            else
                                stillReceiving = false;
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                // Ignore either stream being closed.
                //ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "IOException: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);

            }
            catch (ObjectDisposedException ex)
            {
                // Ignore either stream being closed.
                //ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "ObjectDisposedException: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);

            }
            catch (Exception ex)
            {
                // Log other exceptions.
                if (arguments.DebugMode || System.Diagnostics.Debugger.IsAttached)
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Exception while transmitting data: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                else
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Exception while transmitting data: " + ex.Message, Proxy.LogLevel.Error, LogLevel);
            }
            finally
            {
                // If sending to the local client, log the connection being closed.
                if (!arguments.IsClient)
                {
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, $"IMAP Connection closed after transmitting {FFunc.GetSizeString(bytesTransmitted)}.", Proxy.LogLevel.Information, LogLevel);
                }

                if (clientStream != null)
                    clientStream.Dispose();
                if (remoteServerStream != null)
                    remoteServerStream.Dispose();
            }
        }

        private string DecryptMessage(string messageText)
        {
            byte[] messageTextByte = Encoding.UTF8.GetBytes(messageText);
            MemoryStream mm = new MemoryStream(messageTextByte);

            // keep track of each Stream opened and dispose it in finally section
            var streamTracker = new List<Stream>();

            try
            {
                MimeKit.MimeMessage message = MimeKit.MimeMessage.Load(mm);

                // Bug fix: 跳過 Blocked / Passed Mail List
                if (message.Headers.IndexOf("X-Mailer") >= 0 && message.Headers["X-Mailer"].Equals("Softnext SPAM SQR"))
                    return messageText;


                // ===================================
                // 處理郵件本文
                // ===================================
                /*
                string FixedSignature = "OOOOOOOAAAAAAOOOOOO";
                foreach (var part in message.BodyParts.OfType<TextPart>())
                {
                    int endBodyPos = part.Text.IndexOf("</BODY>", StringComparison.OrdinalIgnoreCase);
                    if (endBodyPos > -1)
                        part.Text = part.Text.Substring(0, endBodyPos) + FixedSignature + part.Text.Substring(endBodyPos);
                    else
                        part.Text += FixedSignature;
                }
                */

                // ===================================
                // 依照 Header資訊處理Message
                // ===================================

                // EncryptHelperTag = 1: 附件檔案有加密，進行解密處理
                // B7C1AC87EAE6BE46E67D084C16B0F3BE
                string hdrName = MD5("EncryptHelperTag");
                if (message.Headers.IndexOf(hdrName) >= 0 && message.Headers[hdrName].Equals(MD5("1")))
                {
                    var iter = new MimeIterator(message);

                    // collect our list of attachments and their parent multiparts
                    while (iter.MoveNext())
                    {
                        var multipart = iter.Parent as Multipart;
                        var part = iter.Current as MimeKit.MimePart;

                        //if (multipart != null && part != null && part.IsAttachment)   // Supernotes 對某些附件沒有添加isattachment flag 
                        if (multipart != null && part != null && part.ContentType.MimeType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
                        {
                            MemoryStream msIn = new MemoryStream();
                            MemoryStream msOut = new MemoryStream();
                            streamTracker.Add(msIn);
                            streamTracker.Add(msOut);

                            part.Content.DecodeTo(msIn);
                            FileDecryptor.Decrypt(msIn, msOut);
                            part.Content = new MimeContent(msOut, ContentEncoding.Default);
                            //測試: 寫入檔案到磁碟 (注意權限)
                            //using (var stream = File.Create(@"files\" + part.ContentType.Name))
                            //    part.Content.DecodeTo(stream);
                        }
                    }
                }


                // 修正系統解密文件 收件人 沒有帶mail的地址被去除
                if (message.Headers.IndexOf("Mail-To") >= 0)
                    message.Headers.Replace("To", message.Headers["Mail-To"]);
                if (message.Headers.IndexOf("Mail-Cc") >= 0)
                    message.Headers.Replace("Cc", message.Headers["Mail-Cc"]);
                if (message.Headers.IndexOf("Mail-Bcc") >= 0)
                    message.Headers.Replace("Bcc", message.Headers["Mail-Bcc"]);

                //using (var stream = File.Create(@"files\" + "aaa.eml"))
                //    message.WriteTo(stream);

                return message.ToString();
            }
            catch (Exception ex)
            {
                ProxyFunctions.Log(LogWriter, SessionId, "", "DecryptMessage Excpetion: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                return string.Empty;
            }
            finally
            {
                if (mm != null)
                    ((IDisposable)mm).Dispose();

                foreach (var s in streamTracker)
                    if (s != null)
                        ((IDisposable)s).Dispose();
            }
        }


        /// <summary>
        /// Process a transmitted message to import any signing certificates for subsequent S/MIME encryption.
        /// </summary>
        /// <param name="o">A ProcessMessageArguments object containing message parameters.</param>
        private void ProcessMessage(object o)
        {
            ProcessMessageArguments arguments = (ProcessMessageArguments)o;

            // Export the message to a local directory.
            if (!string.IsNullOrEmpty(arguments.ExportDirectory))
            {
                string messageId = Functions.ReturnBetween(arguments.MessageText.ToLower(), "message-id: <", ">");
                if (string.IsNullOrEmpty(messageId))
                    messageId = Guid.NewGuid().ToString();

                string fileName = ProxyFunctions.GetExportFileName(arguments.ExportDirectory, messageId, arguments.InstanceId, arguments.UserName);
                File.WriteAllText(fileName, arguments.MessageText);
            }

            // Only parse the message if it contains a known S/MIME content type.
            string canonicalMessageText = arguments.MessageText.ToLower();
            if (canonicalMessageText.IndexOf("application/x-pkcs7-signature") > -1 || canonicalMessageText.IndexOf("application/pkcs7-mime") > -1)
            {
                try
                {
                    // Parse the message.
                    MailMessage message = new MailMessage(arguments.MessageText);

                    // If the message contains a signing certificate that we haven't processed on this session, import it.
                    foreach (X509Certificate2 cert in message.SmimeSigningCertificateChain)
                    {
                        if (cert != null && !SmimeCertificatesReceived.Contains(cert))
                        {
                            // Import the certificate to the Local Machine store.
                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Importing certificate with Serial Number {" + cert.SerialNumber + "}.", Proxy.LogLevel.Information, LogLevel);
                            CertHelper.InstallWindowsCertificate(cert, StoreLocation.LocalMachine);

                            // Remember this ceriticate to avoid importing it again this session.
                            SmimeCertificatesReceived.Add(cert);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (arguments.DebugMode || System.Diagnostics.Debugger.IsAttached)
                        ProxyFunctions.Log(LogWriter, SessionId, "Exception while processing message: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                    else
                        ProxyFunctions.Log(LogWriter, SessionId, "Exception while processing message: " + ex.Message, Proxy.LogLevel.Error, LogLevel);
                }
            }
        }

        /// <summary>
        /// Start an individual IMAP proxy on its own thread.
        /// </summary>
        /// <param name="parameters">ImapProxyArguments object containing all parameters for this connection.</param>
        private static void StartProxy(object parameters)
        {
            ImapProxyArguments arguments = (ImapProxyArguments)parameters;

            // Start the proxy using passed-in settings.
            arguments.Proxy.Start(arguments.AcceptedIPs, arguments.LocalIpAddress, arguments.LocalPort, arguments.LocalEnableSsl, arguments.RemoteServerHostName, arguments.RemoteServerPort, arguments.RemoteServerEnableSsl, arguments.RemoteServerCredential, arguments.ExportDirectory, arguments.LogFile, arguments.LogLevel, arguments.InstanceId, arguments.DebugMode, arguments.LogWriteEnable, arguments.radioClient);
        }

        /// <summary>
        /// Optionally, replace credentials with those from our settings file.
        /// </summary>
        /// <param name="remoteServerStreamWriter">Remote stream writer to transform.</param>
        /// <param name="stringRead">Login string read from the client.</param>
        /// <param name="arguments">A TransmitArguments object containing local and remote server parameters.</param>
        /// <param name="Username">Username of the connected account.</param>
        /// <returns></returns>
        private bool TransformLogin(StreamWriter remoteServerStreamWriter, string stringRead, TransmitArguments arguments, ref string Username)
        {
            string[] commandParts = stringRead.Split(new char[] { ' ' }, 4);
            if (arguments.Credential != null)
            {
                remoteServerStreamWriter.WriteLine(commandParts[0] + " " + commandParts[1] + " " + arguments.Credential.UserName + " " + arguments.Credential.Password);

                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId.ToString(), "C: " + commandParts[0] + " " + commandParts[1] + " " + arguments.Credential.UserName + " " + arguments.Credential.Password, Proxy.LogLevel.Raw, LogLevel);

                return true;
            }
            else
            {
                UserName = commandParts[2].Replace("\"", "");
                return false;
            }
        }
        #endregion Private Methods


        // -----------------------------------------------------------------------------------------------------------------------------
        public static string MD5(string A_0)
        {
            string result = string.Empty;
            try
            {
                byte[] bytes = new UnicodeEncoding().GetBytes(A_0);
                byte[] value = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(bytes);
                result = BitConverter.ToString(value).Replace("-", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine("字符串md5加密錯誤" + ex.Message + ex.StackTrace);
            }
            return result;
        }

    }
}
