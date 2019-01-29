﻿/*
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
using System.Xml.XPath;
using OpaqueMail;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using System.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using ZetaIpc.Runtime.Client;

namespace Freya.Proxy
{
    /// <summary>
    /// SMTP proxy and message transfer agent (MTA) to add or remove S/MIME message signing, encryption, and authentication for outbound messages.
    /// </summary>
    public class SmtpProxy : ProxyBase
    {
        private Dictionary<X509Certificate2, DateTime> CertificateReminders = new Dictionary<X509Certificate2, DateTime>();
        private bool _enable = false;
        public bool proxyrunning { get; private set; } = false;

        #region Public Methods
        /// <summary>
        /// Start an SMTP proxy instance.
        /// </summary>
        /// <param name="acceptedIPs">IP addresses to accept connections from.</param>
        /// <param name="localIPAddress">Local IP address to bind to.</param>
        /// <param name="localPort">Local port to listen on.</param>
        /// <param name="localEnableSsl">Whether the local server supports TLS/SSL.</param>
        /// <param name="remoteServerHostName">Remote server hostname to forward all SMTP messages to.</param>
        /// <param name="remoteServerPort">Remote server port to connect to.</param>
        /// <param name="remoteServerEnableSsl">Whether the remote SMTP server requires TLS/SSL.</param>
        public void Start(string acceptedIPs, IPAddress localIPAddress, int localPort, bool localEnableSsl, string remoteServerHostName, int remoteServerPort, bool remoteServerEnableSsl)
        {
            Start(acceptedIPs, localIPAddress, localPort, localEnableSsl, remoteServerHostName, remoteServerPort, remoteServerEnableSsl, null, "", "", "", "", "", SmimeSettingsMode.BestEffort, true, true, true, true, true, "", "", LogLevel.None, 0, null, false);
        }

        /// <summary>
        /// Start an SMTP proxy instance.
        /// </summary>
        /// <param name="acceptedIPs">IP addresses to accept connections from.</param>
        /// <param name="localIPAddress">Local IP address to bind to.</param>
        /// <param name="localPort">Local port to listen on.</param>
        /// <param name="localEnableSsl">Whether the local server supports TLS/SSL.</param>
        /// <param name="remoteServerHostName">Remote server hostname to forward all SMTP messages to.</param>
        /// <param name="remoteServerPort">Remote server port to connect to.</param>
        /// <param name="remoteServerEnableSsl">Whether the remote SMTP server requires TLS/SSL.</param>
        /// <param name="remoteServerCredential">(Optional) Credentials to be used for all connections to the remote SMTP server.  When set, this overrides any credentials passed locally.</param>
        /// <param name="from">(Optional) "From" address for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="to">(Optional) "To" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="cc">(Optional) "CC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="bcc">(Optional) "BCC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="signature">(Optional) Signature to add to the end of each sent message.</param>
        /// <param name="exportDirectory">(Optional) Location where all outbound messages are saved as EML files.</param>
        /// <param name="logFile">File where event logs and exception information will be written.</param>
        /// <param name="logLevel">Proxy logging level, determining how much information is logged.</param>
        /// <param name="logLevel">Proxy logging level, determining how much information is logged.</param>
        /// <param name="instanceId">The instance number of the proxy.</param>
        public void Start(string acceptedIPs, IPAddress localIPAddress, int localPort, bool localEnableSsl, string remoteServerHostName, int remoteServerPort, bool remoteServerEnableSsl, NetworkCredential remoteServerCredential, string from, string to, string cc, string bcc, string signature, string exportDirectory, string logFile, LogLevel logLevel, int instanceId)
        {
            Start(acceptedIPs, localIPAddress, localPort, localEnableSsl, remoteServerHostName, remoteServerPort, remoteServerEnableSsl, remoteServerCredential, from, to, cc, bcc, signature, SmimeSettingsMode.BestEffort, true, true, true, true, true, exportDirectory, logFile, logLevel, instanceId, new X509Certificate2Collection(), false);
        }

        /// <summary>
        /// Start an SMTP proxy instance.
        /// </summary>
        /// <param name="acceptedIPs">IP addresses to accept connections from.</param>
        /// <param name="localIPAddress">Local IP address to bind to.</param>
        /// <param name="localPort">Local port to listen on.</param>
        /// <param name="localEnableSsl">Whether the local server supports TLS/SSL.</param>
        /// <param name="remoteServerHostName">Remote server hostname to forward all SMTP messages to.</param>
        /// <param name="remoteServerPort">Remote server port to connect to.</param>
        /// <param name="remoteServerEnableSsl">Whether the remote SMTP server requires TLS/SSL.</param>
        /// <param name="remoteServerCredential">(Optional) Credentials to be used for all connections to the remote SMTP server.  When set, this overrides any credentials passed locally.</param>
        /// <param name="from">(Optional) "From" address for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="to">(Optional) "To" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="cc">(Optional) "CC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="bcc">(Optional) "BCC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="signature">(Optional) Signature to add to the end of each sent message.</param>
        /// <param name="smimeSettingsMode">Whether S/MIME settings for encryption and signing are explicitly required or only preferred.</param>
        /// <param name="smimeEncryptedEnvelope">Whether the email's envelope should be encrypted.  When SmimeSign is true, encryption is the second S/MIME operation.</param>
        /// <param name="smimeSigned">Whether the email should be signed.  When true, signing is the first S/MIME operation</param>
        /// <param name="smimeTripleWrapped">Whether the email should be triple-wrapped by signing, then encrypting the envelope, then signing the encrypted envelope.</param>
        /// <param name="smimeRemovePreviousOperations">Remove envelope encryption and signatures from passed-in messages.  If true and SmimeSigned or SmimeEncryptEnvelope is also true, new S/MIME operations will be applied.</param>
        /// <param name="sendCertificateReminders">Send email reminders when a signing certificate is due to expire within 30 days.</param>
        /// <param name="smimeValidCertificates">Collection of certificates to be used when searching for recipient public keys.</param>
        public void Start(string acceptedIPs, IPAddress localIPAddress, int localPort, bool localEnableSsl, string remoteServerHostName, int remoteServerPort, bool remoteServerEnableSsl, NetworkCredential remoteServerCredential, string from, string to, string cc, string bcc, string signature, SmimeSettingsMode smimeSettingsMode, bool smimeSigned, bool smimeEncryptedEnvelope, bool smimeTripleWrapped, bool smimeRemovePreviousOperations, bool sendCertificateReminders, X509Certificate2Collection smimeValidCertificates)
        {
            Start(acceptedIPs, localIPAddress, localPort, localEnableSsl, remoteServerHostName, remoteServerPort, remoteServerEnableSsl, remoteServerCredential, from, to, cc, bcc, signature, smimeSettingsMode, smimeSigned, smimeEncryptedEnvelope, smimeTripleWrapped, smimeRemovePreviousOperations, sendCertificateReminders, "", "", LogLevel.None, 0, smimeValidCertificates, false);
        }

        /// <summary>
        /// Start an SMTP proxy instance.
        /// </summary>
        /// <param name="acceptedIPs">IP addresses to accept connections from.</param>
        /// <param name="localIPAddress">Local IP address to bind to.</param>
        /// <param name="localPort">Local port to listen on.</param>
        /// <param name="localEnableSsl">Whether the local server supports TLS/SSL.</param>
        /// <param name="remoteServerHostName">Remote server hostname to forward all SMTP messages to.</param>
        /// <param name="remoteServerPort">Remote server port to connect to.</param>
        /// <param name="remoteServerEnableSsl">Whether the remote SMTP server requires TLS/SSL.</param>
        /// <param name="remoteServerCredential">(Optional) Credentials to be used for all connections to the remote SMTP server.  When set, this overrides any credentials passed locally.</param>
        /// <param name="from">(Optional) "From" address for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="to">(Optional) "To" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="cc">(Optional) "CC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="bcc">(Optional) "BCC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</param>
        /// <param name="signature">(Optional) Signature to add to the end of each sent message.</param>
        /// <param name="smimeSettingsMode">Whether S/MIME settings for encryption and signing are explicitly required or only preferred.</param>
        /// <param name="smimeEncryptedEnvelope">Whether the email's envelope should be encrypted.  When SmimeSign is true, encryption is the second S/MIME operation.</param>
        /// <param name="smimeSigned">Whether the email should be signed.  When true, signing is the first S/MIME operation</param>
        /// <param name="smimeTripleWrapped">Whether the email should be triple-wrapped by signing, then encrypting the envelope, then signing the encrypted envelope.</param>
        /// <param name="smimeRemovePreviousOperations">Remove envelope encryption and signatures from passed-in messages.  If true and SmimeSigned or SmimeEncryptEnvelope is also true, new S/MIME operations will be applied.</param>
        /// <param name="sendCertificateReminders">Send email reminders when a signing certificate is due to expire within 30 days.</param>
        /// <param name="exportDirectory">(Optional) Location where all outbound messages are saved as EML files.</param>
        /// <param name="logFile">File where event logs and exception information will be written.</param>
        /// <param name="logLevel">Proxy logging level, determining how much information is logged.</param>
        /// <param name="instanceId">The instance number of the proxy.</param>
        /// <param name="smimeValidCertificates">Collection of certificates to be used when searching for recipient public keys.</param>
        /// <param name="debugMode">Whether the proxy instance is running in DEBUG mode and should output full exception messages.</param>
        public void Start(string acceptedIPs, IPAddress localIPAddress, int localPort, bool localEnableSsl, string remoteServerHostName, int remoteServerPort, bool remoteServerEnableSsl, NetworkCredential remoteServerCredential, string from, string to, string cc, string bcc, string signature, SmimeSettingsMode smimeSettingsMode, bool smimeSigned, bool smimeEncryptedEnvelope, bool smimeTripleWrapped, bool smimeRemovePreviousOperations, bool sendCertificateReminders, string exportDirectory, string logFile, LogLevel logLevel, int instanceId, X509Certificate2Collection smimeValidCertificates, bool debugMode, 
            string emailAddr = "", 
            string webService = "", 
            bool LogWriterEnable = false, 
            IpcClient radioClient = null)
        {
            // Create the log writer.
            string logFileName = "";
            if (!string.IsNullOrEmpty(logFile))
            {
                logFileName = ProxyFunctions.GetLogFileName(logFile, instanceId, localIPAddress.ToString(), remoteServerHostName, localPort, remoteServerPort);
                LogWriter = new FreyaStreamWriter(logFileName, true, Encoding.UTF8, Constants.SMALLBUFFERSIZE, radioClient);
                LogWriter.AutoFlush = true;
                LogWriter.textLogEn = LogWriterEnable;
                LogLevel = logLevel;
            }

            // Make sure the remote server isn't an infinite loop back to this server.
            string fqdn = Functions.GetLocalFQDN();
            if (remoteServerHostName.ToUpper() == fqdn.ToUpper() && remoteServerPort == localPort)
            {
                ProxyFunctions.Log(LogWriter, SessionId, "Cannot start service because the remote server host name {" + remoteServerHostName + "} and port {" + remoteServerPort.ToString() + "} is the same as this proxy, which would cause an infinite loop.", Proxy.LogLevel.Critical, LogLevel);
                return;
            }
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress hostIP in hostEntry.AddressList)
            {
                if (remoteServerHostName == hostIP.ToString() && remoteServerPort == localPort)
                {
                    ProxyFunctions.Log(LogWriter, SessionId, "Cannot start service because the remote server hostname {" + remoteServerHostName + "} and port {" + remoteServerPort.ToString() + "} is the same as this proxy, which would cause an infinite loop.", Proxy.LogLevel.Critical, LogLevel);
                    return;
                }
            }

            ProxyFunctions.Log(LogWriter, SessionId, "SMTP Starting service on " + remoteServerHostName + ":" + remoteServerPort, Proxy.LogLevel.Information, LogLevel);

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
                    ProxyFunctions.Log(LogWriter, SessionId, "SMTP Attempting to start for the " + (startAttempts == 2 ? "2nd" : "3rd") + " time.", Proxy.LogLevel.Information, LogLevel);
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

                            ProxyFunctions.Log(LogWriter, SessionId, "New certificate generated with Serial Number {" + serverCertificate.GetSerialNumberString() + "}.", Proxy.LogLevel.Information, LogLevel);
                        }
                    }

                    // Start listening on the specified port and IP address.
                    Listener = new TcpListener(localIPAddress, localPort);
                    Listener.Start();

                    ProxyFunctions.Log(LogWriter, SessionId, "SMTP Service started.", Proxy.LogLevel.Information, LogLevel);
                    ProxyFunctions.Log(LogWriter, SessionId, "SMTP Listening on address {" + localIPAddress.ToString() + "}, port {" + localPort + "}.", Proxy.LogLevel.Information, LogLevel);

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

                        // Prepare the arguments for our new thread.
                        SmtpProxyConnectionArguments arguments = new SmtpProxyConnectionArguments();
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
                        arguments.FixedFrom = from;
                        arguments.FixedTo = to;
                        arguments.FixedCC = cc;
                        arguments.FixedBcc = bcc;
                        arguments.FixedSignature = signature;

                        arguments.SmimeSettingsMode = smimeSettingsMode;
                        arguments.SmimeSigned = smimeSigned;
                        arguments.SmimeEncryptedEnvelope = smimeEncryptedEnvelope;
                        arguments.SmimeTripleWrapped = smimeTripleWrapped;
                        arguments.SmimeRemovePreviousOperations = smimeRemovePreviousOperations;

                        arguments.SendCertificateReminders = sendCertificateReminders;

                        arguments.SmimeValidCertificates = smimeValidCertificates;

                        // Increment the connection counter;
                        arguments.ConnectionId = (unchecked(++ConnectionId)).ToString();
                        arguments.InstanceId = instanceId;
                        arguments.DebugMode = debugMode;

                        arguments.EMailAddr = emailAddr;
                        arguments.WebService = webService;

                        // Fork the thread and continue listening for new connections.
                        Thread processThread = new Thread(new ParameterizedThreadStart(ProcessConnection));
                        processThread.Name = "Freya SMTP Proxy Connection";
                        processThread.Start(arguments);
                    }
                    return;
                }
                catch (SocketException ex)
                {
                    if ((ex.SocketErrorCode == SocketError.Interrupted))
                        // a blocking listen has been cancelled
                        ProxyFunctions.Log(LogWriter, SessionId, "SMTP Listening thread: Receive Stop command, cancel TCPListener.", Proxy.LogLevel.Critical, LogLevel);
                    else
                    {
                        if (debugMode || System.Diagnostics.Debugger.IsAttached)
                            ProxyFunctions.Log(LogWriter, SessionId, "Exception when starting SMTP proxy: " + ex.ToString(), Proxy.LogLevel.Critical, LogLevel);
                        else
                            ProxyFunctions.Log(LogWriter, SessionId, "Exception when starting SMTP proxy: " + ex.Message, Proxy.LogLevel.Critical, LogLevel);
                    }
                }
            }
            proxyrunning = false;
        }

        /// <summary>
        /// Stop the SMTP proxy and close all existing connections.
        /// </summary>
        public void Stop()
        {
            ProxyFunctions.Log(LogWriter, SessionId, "SMTP Stopping service.", Proxy.LogLevel.Information, LogLevel);

            while (proxyrunning)
            {
                _enable = false;
                Started = false;

                if (Listener != null)
                    Listener.Stop();
            }

            ProxyFunctions.Log(LogWriter, SessionId, "SMTP Service stopped.", Proxy.LogLevel.Information, LogLevel);
        }

        /// <summary>
        /// Start all SMTP proxy instances from the specified settings file.
        /// </summary>
        /// <param name="fileName">File containing the SMTP proxy settings.</param>
        public static List<SmtpProxy> StartProxiesFromSettingsFile(string fileName)
        {
            List<SmtpProxy> smtpProxies = new List<SmtpProxy>();

            try
            {
                if (File.Exists(fileName))
                {
                    XPathDocument document = new XPathDocument(fileName);
                    XPathNavigator navigator = document.CreateNavigator();

                    int smtpServiceCount = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/SMTP/ServiceCount");
                    for (int i = 1; i <= smtpServiceCount; i++)
                    {
                        SmtpProxyArguments arguments = new SmtpProxyArguments();
                        arguments.AcceptedIPs = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/AcceptedIPs");

                        string localIpAddress = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/LocalIPAddress").ToUpper();
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

                        arguments.LocalPort = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/SMTP/Service" + i + "/LocalPort");
                        // If the port is invalid, proceed to the next service instance.
                        if (arguments.LocalPort < 1)
                            continue;

                        arguments.LocalEnableSsl = ProxyFunctions.GetXmlBoolValue(navigator, "/Settings/SMTP/Service" + i + "/LocalEnableSSL");

                        arguments.RemoteServerHostName = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/RemoteServerHostName");
                        // If the host name is invalid, proceed to the next service instance.
                        if (string.IsNullOrEmpty(arguments.RemoteServerHostName))
                            continue;

                        arguments.RemoteServerPort = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/SMTP/Service" + i + "/RemoteServerPort");
                        // If the port is invalid, proceed to the next service instance.
                        if (arguments.RemoteServerPort < 1)
                            continue;

                        arguments.RemoteServerEnableSsl = ProxyFunctions.GetXmlBoolValue(navigator, "/Settings/SMTP/Service" + i + "/RemoteServerEnableSSL");

                        string remoteServerUsername = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/RemoteServerUsername");
                        if (!string.IsNullOrEmpty(remoteServerUsername))
                        {
                            arguments.RemoteServerCredential = new NetworkCredential();
                            arguments.RemoteServerCredential.UserName = remoteServerUsername;
                            arguments.RemoteServerCredential.Password = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/RemoteServerPassword");
                        }

                        arguments.FixedFrom = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/From");
                        arguments.FixedTo = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/To");
                        arguments.FixedCC = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/CC");
                        arguments.FixedBcc = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/BCC");
                        arguments.FixedSignature = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/Signature");

                        string certificateLocationValue = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/Certificate/Location");
                        StoreLocation certificateLocation = StoreLocation.LocalMachine;
                        if (certificateLocationValue.ToUpper() == "CURRENTUSER")
                            certificateLocation = StoreLocation.CurrentUser;

                        // Try to load the signing certificate based on its serial number first, then fallback to its subject name.
                        string certificateValue = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/Certificate/SerialNumber");
                        if (!string.IsNullOrEmpty(certificateValue))
                            arguments.Certificate = CertHelper.GetCertificateBySerialNumber(certificateLocation, certificateValue);
                        else
                        {
                            certificateValue = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/Certificate/SubjectName");
                            if (!string.IsNullOrEmpty(certificateValue))
                                arguments.Certificate = CertHelper.GetCertificateBySubjectName(certificateLocation, certificateValue);
                        }

                        arguments.SendCertificateReminders = ProxyFunctions.GetXmlBoolValue(navigator, "Settings/SMTP/Service" + i + "/SendCertificateReminders");

                        arguments.SmimeEncryptedEnvelope = ProxyFunctions.GetXmlBoolValue(navigator, "Settings/SMTP/Service" + i + "/SMIMEEncrypt");
                        arguments.SmimeRemovePreviousOperations = ProxyFunctions.GetXmlBoolValue(navigator, "Settings/SMTP/Service" + i + "/SMIMERemovePreviousOperations");
                        arguments.SmimeSigned = ProxyFunctions.GetXmlBoolValue(navigator, "Settings/SMTP/Service" + i + "/SMIMESign");
                        arguments.SmimeTripleWrapped = ProxyFunctions.GetXmlBoolValue(navigator, "Settings/SMTP/Service" + i + "/SMIMETripleWrap");

                        // Look up the S/MIME settings mode, defaulting to requiring exact settings.
                        string smimeSettingsMode = ProxyFunctions.GetXmlStringValue(navigator, "Settings/SMTP/Service" + i + "/SMIMESettingsMode");
                        if (smimeSettingsMode.ToUpper() == "BESTEFFORT")
                            arguments.SmimeSettingsMode = SmimeSettingsMode.BestEffort;
                        else
                            arguments.SmimeSettingsMode = SmimeSettingsMode.RequireExactSettings;

                        int publicKeyCount = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/SMTP/Service" + i + "/PublicKeyCount");
                        if (publicKeyCount > 0)
                        {
                            arguments.SmimeValidCertificates = new X509Certificate2Collection();
                            for (int j = 1; j <= publicKeyCount; j++)
                            {
                                string publicKey = ProxyFunctions.GetXmlStringValue(navigator, "/Settings/SMTP/Service" + i + "/PublicKey" + j);
                                if (!string.IsNullOrEmpty(publicKey))
                                {
                                    try
                                    {
                                        X509Certificate2 cert = new X509Certificate2();
                                        cert.Import(Functions.FromBase64(publicKey));
                                        arguments.SmimeValidCertificates.Add(cert);
                                    }
                                    catch { }
                                }
                            }
                        }

                        arguments.ExportDirectory = ProxyFunctions.GetXmlStringValue(navigator, "Settings/SMTP/Service" + i + "/ExportDirectory");
                        arguments.LogFile = ProxyFunctions.GetXmlStringValue(navigator, "Settings/SMTP/Service" + i + "/LogFile");

                        string logLevel = ProxyFunctions.GetXmlStringValue(navigator, "Settings/SMTP/Service" + i + "/LogLevel");
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
                        arguments.DebugMode = ProxyFunctions.GetXmlBoolValue(navigator, "Settings/SMTP/Service" + i + "/Debug");

                        // Remember the proxy in order to close it when the service stops.
                        arguments.Proxy = new SmtpProxy();
                        smtpProxies.Add(arguments.Proxy);

                        Thread proxyThread = new Thread(new ParameterizedThreadStart(StartProxy));
                        proxyThread.Name = "Freya SMTP Proxy";
                        proxyThread.Start(arguments);
                    }
                }
            }
            catch
            {
            }

            return smtpProxies;
        }

        public static List<SmtpProxy> StartProxiesFromRegistry(FRegSetting reg, IpcClient radioClient = null)
        {
            List<SmtpProxy> smtpProxies = new List<SmtpProxy>();

            try
            {
                //int smtpServiceCount = ProxyFunctions.GetXmlIntValue(navigator, "/Settings/SMTP/ServiceCount");
                int smtpServiceCount = 1; //暫時僅支援1個proxy
                for (int i = 1; i <= smtpServiceCount; i++)
                {
                    SmtpProxyArguments arguments = new SmtpProxyArguments();
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

                    // Change to 587 for highack supernote, and enable SSL
                    arguments.LocalPort = 25;
                    // If the port is invalid, proceed to the next service instance.
                    if (arguments.LocalPort < 1)
                        continue;

                    arguments.LocalEnableSsl = false;

                    arguments.RemoteServerHostName = reg.SMTPServerIP;
                    // If the host name is invalid, proceed to the next service instance.
                    if (string.IsNullOrEmpty(arguments.RemoteServerHostName))
                        continue;

                    arguments.RemoteServerPort = 587;
                    // If the port is invalid, proceed to the next service instance.
                    if (arguments.RemoteServerPort < 1)
                        continue;

                    arguments.RemoteServerEnableSsl = true;

                    string remoteServerUsername = "";
                    if (!string.IsNullOrEmpty(remoteServerUsername))
                    {
                        arguments.RemoteServerCredential = new NetworkCredential();
                        arguments.RemoteServerCredential.UserName = remoteServerUsername;
                        arguments.RemoteServerCredential.Password = "";
                    }

                    arguments.FixedFrom = "";
                    arguments.FixedTo = "";
                    arguments.FixedCC = "";
                    arguments.FixedBcc = "";
                    arguments.FixedSignature = "";

                    string certificateLocationValue = "LocalMachine";
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

                    arguments.SendCertificateReminders = false;

                    arguments.SmimeEncryptedEnvelope = false;
                    arguments.SmimeRemovePreviousOperations = false;
                    arguments.SmimeSigned = false;
                    arguments.SmimeTripleWrapped = false;

                    // Look up the S/MIME settings mode, defaulting to requiring exact settings.
                    string smimeSettingsMode = "BestEffort";
                    if (smimeSettingsMode.ToUpper() == "BESTEFFORT")
                        arguments.SmimeSettingsMode = SmimeSettingsMode.BestEffort;
                    else
                        arguments.SmimeSettingsMode = SmimeSettingsMode.RequireExactSettings;

                    int publicKeyCount = 0;
                    if (publicKeyCount > 0)
                    {
                        arguments.SmimeValidCertificates = new X509Certificate2Collection();
                        for (int j = 1; j <= publicKeyCount; j++)
                        {
                            string publicKey = "";
                            if (!string.IsNullOrEmpty(publicKey))
                            {
                                try
                                {
                                    X509Certificate2 cert = new X509Certificate2();
                                    cert.Import(Functions.FromBase64(publicKey));
                                    arguments.SmimeValidCertificates.Add(cert);
                                }
                                catch { }
                            }
                        }
                    }

                    arguments.ExportDirectory = @"";
                    arguments.LogFile = @"Logs\SMTPProxy{#}-{yyyy-MM-dd}.log";
                    //arguments.LogFile = "";  //沒有logfile name logwriter會不能動，連UI log都沒有，代處理

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

                    arguments.EMailAddr = reg.EMail;
                    arguments.WebService = reg.WebServiceIP;

                    arguments.LogWriteEnable = reg.SMTPLogWriterEnable;
                    arguments.radioClient = radioClient;

                    // Remember the proxy in order to close it when the service stops.
                    arguments.Proxy = new SmtpProxy();
                    smtpProxies.Add(arguments.Proxy);

                    Thread proxyThread = new Thread(new ParameterizedThreadStart(StartProxy));
                    proxyThread.Name = "Freya SMTP Proxy";
                    proxyThread.Start(arguments);
                }

            }
            catch { }

            return smtpProxies;
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Handle an incoming SMTP connection, from connection to completion.
        /// </summary>
        /// <param name="parameters">SmtpProxyConnectionArguments object containing all parameters for this connection.</param>
        private async void ProcessConnection(object parameters)
        {
            // Cast the passed-in parameters back to their original objects.
            SmtpProxyConnectionArguments arguments = (SmtpProxyConnectionArguments)parameters;

            // The overall number of bytes transmitted on this connection.
            ulong bytesTransmitted = 0;

            TcpClient client = null;
            Stream clientStream = null;
            StreamReader clientStreamReader = null;
            StreamWriter clientStreamWriter = null;

            string ip = "";

            try
            {
                client = arguments.TcpClient;
                clientStream = client.GetStream();

                // Placeholder variables to be populated throughout the client session.
                NetworkCredential credential = arguments.RemoteServerCredential;
                string fromAddress = "";
                string identity = "";
                List<MailboxAddress> toList = new List<MailboxAddress>();
                bool sending = false, inPlainAuth = false, inLoginAuth = false;

                // A byte array to streamline bit shuffling.
                char[] buffer = new char[Constants.SMALLBUFFERSIZE];

                // Capture the client's IP information.
                PropertyInfo pi = clientStream.GetType().GetProperty("Socket", BindingFlags.NonPublic | BindingFlags.Instance);
                ip = ((Socket)pi.GetValue(clientStream, null)).RemoteEndPoint.ToString();
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

                clientStreamReader = new StreamReader(clientStream);
                clientStreamWriter = new StreamWriter(clientStream);
                clientStreamWriter.AutoFlush = true;

                // Validate that the IP address is within an accepted range.
                if (!ProxyFunctions.ValidateIP(arguments.AcceptedIPs, ip))
                {
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Connection rejected from {" + ip + "} due to its IP address.", Proxy.LogLevel.Warning, LogLevel);

                    await Functions.SendStreamStringAsync(clientStreamWriter, "500 IP address [" + ip + "] rejected.\r\n");
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 500 IP address [" + ip + "] rejected.", Proxy.LogLevel.Raw, LogLevel);

                    if (clientStream != null)
                        clientStream.Dispose();
                    if (client != null)
                        client.Close();

                    return;
                }

                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "New SMTP connection established from {" + ip + "}.", Proxy.LogLevel.Information, LogLevel);

                // Send our welcome message.
                await Functions.SendStreamStringAsync(clientStreamWriter, "220 " + WelcomeMessage + "\r\n");
                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "220 " + WelcomeMessage, Proxy.LogLevel.Raw, LogLevel);

                // Instantiate an SmtpClient for sending messages to the remote server.
                //using (OpaqueMail.SmtpClient smtpClient = new OpaqueMail.SmtpClient(arguments.RemoteServerHostName, arguments.RemoteServerPort))
                //using (var smtpClient = new MailKit.Net.Smtp.SmtpClient(new ProtocolLogger(Console.OpenStandardOutput())))
                using (var smtpClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    //smtpClient.EnableSsl = arguments.RemoteServerEnableSsl;
                    smtpClient.Credentials = arguments.RemoteServerCredential;

                    //if (arguments.SmimeValidCertificates != null)
                    //    smtpClient.SmimeValidCertificates = arguments.SmimeValidCertificates;

                    // Loop through each received command.
                    string command = "";
                    bool stillReceiving = true;
                    while (Started && stillReceiving)
                    {
                        int bytesRead = await clientStreamReader.ReadAsync(buffer, 0, Constants.SMALLBUFFERSIZE);

                        if (bytesRead > 0)
                        {
                            bytesTransmitted += (ulong)bytesRead;

                            command += new string(buffer, 0, bytesRead);

                            if (command.EndsWith("\r\n"))
                            {
                                // Handle continuations of current "DATA" commands.
                                if (sending)
                                {
                                    // Handle the finalization of a "DATA" command.
                                    if (command.EndsWith("\r\n.\r\n"))
                                    {
                                        sending = false;

                                        string messageFrom = "", messageSubject = "", messageSize = "";
                                        try
                                        {
                                            string messageText = command.Substring(0, command.Length - 5);

                                            // Export the message to a local directory.

                                            if (!string.IsNullOrEmpty(arguments.ExportDirectory))
                                            {
                                                string messageId = Functions.ReturnBetween(messageText.ToLower(), "message-id: <", ">");
                                                if (string.IsNullOrEmpty(messageId))
                                                    messageId = Guid.NewGuid().ToString();

                                                string userName = "";
                                                if (smtpClient.Credentials != null)
                                                    userName = ((NetworkCredential)smtpClient.Credentials).UserName;

                                                string fileName = ProxyFunctions.GetExportFileName(arguments.ExportDirectory, messageId, arguments.InstanceId, userName);
                                                File.WriteAllText(fileName, messageText);
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "FileDumped: " + fileName, Proxy.LogLevel.Verbose, LogLevel);

                                            }

                                            //MailMessage message = new MailMessage(messageText, MailMessageProcessingFlags.IncludeRawHeaders | MailMessageProcessingFlags.IncludeRawBody);
                                            byte[] messageTextByte = Encoding.UTF8.GetBytes(messageText);
                                            MemoryStream mm = new MemoryStream(messageTextByte);
                                            MimeKit.MimeMessage message = MimeKit.MimeMessage.Load(mm);

                                            if (message.Headers.IndexOf("Subject") < 0)
                                                message.Headers.Replace("Subject", ""); //萬一郵件沒有主旨，.Subject是null，後面會出錯

                                            /*
                                            if (!string.IsNullOrEmpty(arguments.FixedFrom))
                                            {
                                                message.From = MailAddressCollection.Parse(arguments.FixedFrom)[0];

                                                if (message.RawHeaders.Contains("From: "))
                                                    message.RawHeaders = Functions.ReplaceBetween(message.RawHeaders, "From: ", "\r\n", Functions.ToMailAddressString(message.From));
                                                else
                                                    message.RawHeaders = message.RawHeaders.Replace("\r\nSubject: ", "\r\nFrom: " + Functions.ToMailAddressString(message.From) + "\r\nSubject: ");
                                            }

                                            if (!string.IsNullOrEmpty(arguments.FixedTo))
                                            {
                                                MailAddressCollection addresses = MailAddressCollection.Parse(arguments.FixedTo);
                                                foreach (MailAddress address in addresses)
                                                {
                                                    bool addressFound = false;
                                                    foreach (MailAddress existingAddress in message.To)
                                                    {
                                                        if (existingAddress.Address.ToUpper() == address.Address.ToUpper())
                                                            addressFound = true;
                                                    }

                                                    if (!addressFound)
                                                        message.To.Add(address);
                                                }

                                                if (message.RawHeaders.Contains("To: "))
                                                    message.RawHeaders = Functions.ReplaceBetween(message.RawHeaders, "To: ", "\r\n", message.To.ToString());
                                                else
                                                    message.RawHeaders = message.RawHeaders.Replace("\r\nSubject: ", "\r\nTo: " + message.To.ToString() + "\r\nSubject: ");
                                            }

                                            if (!string.IsNullOrEmpty(arguments.FixedCC))
                                            {
                                                MailAddressCollection addresses = MailAddressCollection.Parse(arguments.FixedCC);
                                                foreach (MailAddress address in addresses)
                                                {
                                                    bool addressFound = false;
                                                    foreach (MailAddress existingAddress in message.CC)
                                                    {
                                                        if (existingAddress.Address.ToUpper() == address.Address.ToUpper())
                                                            addressFound = true;
                                                    }

                                                    if (!addressFound)
                                                        message.CC.Add(address);
                                                }

                                                if (message.RawHeaders.Contains("CC: "))
                                                    message.RawHeaders = Functions.ReplaceBetween(message.RawHeaders, "CC: ", "\r\n", message.To.ToString());
                                                else
                                                    message.RawHeaders = message.RawHeaders.Replace("\r\nSubject: ", "\r\nTo: " + message.To + "\r\nSubject: ");
                                            }

                                            if (!string.IsNullOrEmpty(arguments.FixedBcc))
                                            {
                                                MailAddressCollection addresses = MailAddressCollection.Parse(arguments.FixedBcc);
                                                foreach (MailAddress address in addresses)
                                                {
                                                    bool addressFound = false;
                                                    foreach (MailAddress existingAddress in message.Bcc)
                                                    {
                                                        if (existingAddress.Address.ToUpper() == address.Address.ToUpper())
                                                            addressFound = true;
                                                    }

                                                    if (!addressFound)
                                                        message.Bcc.Add(address);
                                                }
                                            }

                                            // Insert the fixed signature if one exists.
                                            if (!string.IsNullOrEmpty(arguments.FixedSignature))
                                            {
                                                int endBodyPos = message.Body.IndexOf("</BODY>", StringComparison.OrdinalIgnoreCase);
                                                if (endBodyPos > -1)
                                                    message.Body = message.Body.Substring(0, endBodyPos) + arguments.FixedSignature + message.Body.Substring(endBodyPos);
                                                else
                                                    message.Body += arguments.FixedSignature;
                                            }

                                            */

                                            // If the received message is already signed or encrypted and we don't want to remove previous S/MIME operations, forward it as-is.
                                            //string contentType = message.ContentType;
                                            string contentType = message.Body.ContentType.ToString();
                                            if ((contentType.StartsWith("application/pkcs7-mime") || contentType.StartsWith("application/x-pkcs7-mime") || contentType.StartsWith("application/x-pkcs7-signature")) && !arguments.SmimeRemovePreviousOperations)
                                            {

                                                //message.SmimeSigned = message.SmimeEncryptedEnvelope = message.SmimeTripleWrapped = false;
                                                await smtpClient.SendAsync(message);
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: " + message, Proxy.LogLevel.Raw, LogLevel);

                                            }
                                            else
                                            {
                                                try
                                                {
                                                    //精簡Subject
                                                    message.Subject = message.Subject.Replace("：", ":");
                                                    Regex ReRegex;
                                                    if (message.Subject.Length > 0)
                                                    {
                                                        int nRe = 0, cRe = 0;
                                                        while ((nRe = message.Subject.IndexOf(':', nRe + 1)) != -1)
                                                            if (cRe++ >= 1) break;  //保留兩個
                                                        if (nRe != -1)
                                                        {
                                                            ReRegex = new Regex(@"(?>(?:re|fwd|fw|轉呈|转寄|回信|回复) *: *)*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                                            message.Subject = message.Subject.Substring(0, nRe + 1).Trim() + " " + ReRegex.Replace(message.Subject, "").Trim();
                                                        }
                                                    }


                                                    //刪除body中不必要的文字
                                                    string dummystr = "";
                                                    foreach (var part in message.BodyParts.OfType<TextPart>())
                                                    {
                                                        // Legle statement
                                                        do
                                                        {
                                                            dummystr = part.Text.Between("⌘", "⌘");
                                                            if (dummystr == "") break;
                                                            part.Text = part.Text.Replace("⌘" + dummystr + "⌘", "");
                                                        } while (dummystr.Length > 0);

                                                        // Mail From
                                                        /*
                                                        do
                                                        {
                                                            dummystr = part.Text.Between("mail from ip", "1.6.9.8B");
                                                            if (dummystr == "") break;
                                                            part.Text = part.Text.Replace("mail from ip" + dummystr + "1.6.9.8B", "");
                                                        } while (dummystr.Length > 0);
                                                        */
                                                        ReRegex = new Regex(@"(mail\s*from\s*ip[\S\s]*1\.6\.9\.8B)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                                        part.Text = ReRegex.Replace(part.Text, "");

                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "SMTP: Filter Subject / Message Excpetion -  " + e.ToString(), Proxy.LogLevel.Error, LogLevel);
                                                }



                                                //messageFrom = message.From.Address;
                                                //messageSubject = message.Subject;
                                                //messageSize = message.Size.ToString("N0");
                                                messageFrom = message.From.ToString();
                                                messageSubject = message.Subject.ToString();


                                                //ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Forwarding message from {" + messageFrom + "} with subject {" + message.Subject + "} and size of {" + message.Size.ToString("N0") + "}.", Proxy.LogLevel.Verbose, LogLevel);
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Forwarding message from {" + messageFrom + "} with subject {" + message.Subject + "}.", Proxy.LogLevel.Information, LogLevel);

                                                //BCC header有些軟體不送，自己加入
                                                foreach (MailboxAddress toListAddress in toList)
                                                {
                                                    bool addressFound = false;

                                                    foreach (MailboxAddress addr in message.To)
                                                    {
                                                        if (addr.Address.Equals(toListAddress.Address))
                                                        {
                                                            addressFound = true;
                                                            break;
                                                        }
                                                    }
                                                    if (addressFound)
                                                        continue;

                                                    foreach (MailboxAddress addr in message.Cc)
                                                    {
                                                        if (addr.Address.Equals(toListAddress.Address))
                                                        {
                                                            addressFound = true;
                                                            break;
                                                        }
                                                    }
                                                    if (addressFound)
                                                        continue;

                                                    foreach (MailboxAddress addr in message.Bcc)
                                                    {
                                                        if (addr.Address.Equals(toListAddress.Address))
                                                        {
                                                            addressFound = true;
                                                            break;
                                                        }
                                                    }
                                                    if (addressFound)
                                                        continue;

                                                    message.Bcc.Add(toListAddress);
                                                }


                                                /*
                                                // Attempt to sign and encrypt the envelopes of all messages, but still send if unable to.
                                                message.SmimeSettingsMode = SmimeSettingsMode.BestEffort;

                                                // Apply S/MIME settings.
                                                message.SmimeSigned = arguments.SmimeSigned;
                                                message.SmimeEncryptedEnvelope = arguments.SmimeEncryptedEnvelope;
                                                message.SmimeTripleWrapped = arguments.SmimeTripleWrapped;

                                                // Look up the S/MIME signing certificate for the current sender.  If it doesn't exist, create one.
                                                if (message.SmimeSigned || message.SmimeTripleWrapped)
                                                {
                                                    message.SmimeSigningCertificate = CertHelper.GetCertificateBySubjectName(StoreLocation.LocalMachine, message.From.Address);
                                                    if (message.SmimeSigningCertificate == null)
                                                        message.SmimeSigningCertificate = CertHelper.CreateSelfSignedCertificate("E=" + message.From.Address, message.From.Address, StoreLocation.LocalMachine, true, 4096, 10);
                                                }
                                                */

                                                ///
                                                // Send the message.
                                                ///

                                                ///
                                                //先做webServer認證
                                                ///
                                                if (arguments.WebService.Length > 0)
                                                {
                                                    bool hasattachment = false;
                                                    foreach (var attachment in message.Attachments)
                                                    {
                                                        if (!(attachment is MessagePart))
                                                        {
                                                            hasattachment = true;
                                                        }
                                                    }
                                                    if (!hasattachment)
                                                        message.Headers.RemoveAll("X-Has-Attach");

                                                    string webServiceResults = null;
                                                    List<string> receiverEmails = new List<string>();
                                                    foreach (MailboxAddress toAddr in message.To)
                                                    {
                                                        receiverEmails.Add(toAddr.Address);
                                                    }

                                                    string authstr = string.Format("<approvalRequest><senderEmail>{0}</senderEmail><receiverEmails>{1}</receiverEmails><curVersion>1.6.9.8</curVersion><hasAttachment>{2}</hasAttachment><hasApprovalKeyword>false</hasApprovalKeyword><toEmail>{1}</toEmail><ccEmail>{3}</ccEmail><bccEmail>{4}</bccEmail></approvalRequest>"
                                                                , arguments.EMailAddr
                                                                , GetAddressListfromMessage(message.To)
                                                                , hasattachment.ToString().ToLower()
                                                                , GetAddressListfromMessage(message.Cc)
                                                                , GetAddressListfromMessage(message.Bcc)
                                                                );

                                                    using (WebClient wc = new WebClient())
                                                    {
                                                        try
                                                        {
                                                            wc.Encoding = Encoding.UTF8;
                                                            wc.Headers.Add(HttpRequestHeader.ContentType, "text/plain");
                                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "PUT: " + "http://" + arguments.WebService + "/superNotesWS/approval/" + arguments.EMailAddr + "\r\n", Proxy.LogLevel.Raw, LogLevel);
                                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "PUT: " + authstr + "\r\n\r\n", Proxy.LogLevel.Raw, LogLevel);
                                                            webServiceResults = wc.UploadString("http://" + arguments.WebService + "/superNotesWS/approval/" + arguments.EMailAddr, "PUT", authstr);
                                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, webServiceResults + "\r\n", Proxy.LogLevel.Verbose, LogLevel);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "SomthingWrong: " + e.ToString(), Proxy.LogLevel.Error, LogLevel);
                                                        }
                                                    }

                                                    //依照Webservice回應修改header
                                                    if (!(message.Headers.IndexOf("X-Mailer") >= 0 ? message.Headers["X-Mailer"].Equals("Super Notes 1.6.9.8B") : false))
                                                    {//非SuperNotes才要修改 Header
                                                        message.Headers.Replace("X-Mailer", "Super Notes 1.6.9.8B");
                                                        message.Headers.Replace("Sender", ((MailboxAddress)message.From.First()).Address);
                                                        message.Headers.Replace("PriorityFlag", "Normal");
                                                        message.Headers.Replace("ConfidentialityFlag", "Normal");
                                                        message.Headers.Replace("MoodMark", "0");

                                                        message.Headers.RemoveAll("User-Agent");

                                                        XmlDocument doc = new XmlDocument();
                                                        doc.LoadXml(webServiceResults);
                                                        string strbuf;

                                                        strbuf = doc.GetElementsByTagName("cctype")[0]?.InnerText;
                                                        if (strbuf != null) message.Headers.Replace("CCType", strbuf);

                                                        //Internet Mail需要審核 (FORWARD)
                                                        if (doc.GetElementsByTagName("status")[0]?.InnerText.Equals("FORWARD") == true)
                                                        {
                                                            strbuf = doc.GetElementsByTagName("cclist")[0]?.InnerText;
                                                            if (strbuf != null) message.Headers.Replace("CCList", strbuf);

                                                            strbuf = doc.GetElementsByTagName("BGApprover")[0]?.InnerText;
                                                            if (strbuf != null) message.Headers.Replace("csnewbg", strbuf);

                                                            strbuf = doc.GetElementsByTagName("ApprovalBG")[0]?.InnerText;
                                                            if (strbuf != null) message.Headers.Replace("csbg", strbuf);

                                                            strbuf = doc.GetElementsByTagName("ApprovalBU")[0]?.InnerText;
                                                            if (strbuf != null) message.Headers.Replace("csbu", strbuf);

                                                            strbuf = doc.GetElementsByTagName("isInternetMail")[0]?.InnerText;
                                                            if (strbuf != null) message.Headers.Replace("csflag", strbuf);

                                                            strbuf = doc.GetElementsByTagName("isInternetSenderWhiteList")[0]?.InnerText;
                                                            if (strbuf != null) message.Headers.Replace("cswhite", strbuf);

                                                            strbuf = doc.GetElementsByTagName("approver")[0]?.InnerText;
                                                            //strbuf = "chia-wen.huang@mail.foxconn.com";
                                                            if (strbuf != null)
                                                            {
                                                                message.Headers.Replace("internetapprover", strbuf);
                                                                message.Headers.Replace("savEnterSendTo", message.Headers["To"]);   // Actual To
                                                                if (message.Headers.Contains("Cc"))
                                                                    message.Headers.Replace("savEnterCopyTo", message.Headers["Cc"]);   // Actual CC
                                                                if (message.Headers.Contains("Bcc"))
                                                                    message.Headers.Replace("savEnterBlindTo", message.Headers["Bcc"]);  // Actual BCC
                                                                message.Headers.Replace("savSubject", message.Subject);             // Actual subject
                                                                message.Headers.Replace("To", strbuf);
                                                                message.Headers.RemoveAll("Cc");
                                                                message.Headers.RemoveAll("Bcc");
                                                                message.Subject = "[Approve Required]" + message.Subject;
                                                            }
                                                        }
                                                    }
                                                }

                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "C: HeaderList:\r\n" + string.Join("\r\n", message.Headers), Proxy.LogLevel.Verbose, LogLevel);
                                                //ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "C: MessageTextBody: -------------------------------------\r\n" + message.TextBody + "\r\n-----------------------------------------------------\r\n", Proxy.LogLevel.Raw, LogLevel);
                                                //ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "C: Message: -------------------------------------\r\n" + message.ToString() + "\r\n-----------------------------------------------------\r\n", Proxy.LogLevel.Raw, LogLevel);
                                                
                                                smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;  //ignore SSL certification
                                                smtpClient.LocalDomain = "ismetoad";    // for Foxconn internal SMTP server
                                                smtpClient.Connect(arguments.RemoteServerHostName, arguments.RemoteServerPort, false);
                                                //smtpClient.Authenticate("xxx@gmail.com", "password");
                                                smtpClient.Send(message);
                                                smtpClient.Disconnect(true);


                                                if (mm != null)
                                                    ((IDisposable)mm).Dispose();

                                            }

                                            await Functions.SendStreamStringAsync(clientStreamWriter, "250 Forwarded\r\n");
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 250 Forwarded", Proxy.LogLevel.Raw, LogLevel);

                                            //ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Message from {" + message.From.ToString() + "} with subject {" + message.Subject + "} and size of {" + message.Size.ToString("N0") + "} successfully forwarded.", Proxy.LogLevel.Verbose, LogLevel);
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Message from {" + message.From.ToString() + "} with subject {" + message.Subject + "} and size of {" + "---" + "} successfully forwarded.", Proxy.LogLevel.Verbose, LogLevel);
                                        }
                                        catch (Exception ex)
                                        {
                                            // Report if an exception was encountering sending the message.
                                            Functions.SendStreamString(clientStreamWriter, "500 Error occurred when forwarding\r\n");
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 500 Error occurred when forwarding", Proxy.LogLevel.Raw, LogLevel);

                                            if (arguments.DebugMode || System.Diagnostics.Debugger.IsAttached)
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Error when forwarding message from {" + messageFrom + "} with subject {" + messageSubject + "} and size of {" + messageSize + "}.  Exception: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                                            else
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Error when forwarding message from {" + messageFrom + "} with subject {" + messageSubject + "} and size of {" + messageSize + "}.  Exception: " + ex.Message, Proxy.LogLevel.Error, LogLevel);
                                        }
                                        command = "";
                                    }
                                }
                                else
                                {
                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "C: " + new string(buffer, 0, bytesRead), Proxy.LogLevel.Raw, LogLevel);

                                    // Handle continuations of current "AUTH PLAIN" commands.
                                    if (inPlainAuth)
                                    {
                                        inPlainAuth = false;
                                        // Split up an AUTH PLAIN handshake into its components.
                                        string authString = Encoding.UTF8.GetString(Convert.FromBase64String(command));
                                        string[] authStringParts = authString.Split(new char[] { '\0' }, 3);
                                        if (authStringParts.Length > 2 && arguments.RemoteServerCredential == null)
                                            smtpClient.Credentials = new NetworkCredential(authStringParts[1], authStringParts[2]);

                                        await Functions.SendStreamStringAsync(clientStreamWriter, "235 OK\r\n");
                                        ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 235 OK", Proxy.LogLevel.Raw, LogLevel);

                                        command = "";
                                    }
                                    // Handle continuations of current "AUTH LOGIN" commands.
                                    else if (inLoginAuth)
                                    {
                                        if (smtpClient.Credentials == null)
                                        {
                                            // Handle the username being received for the first time.
                                            smtpClient.Credentials = new NetworkCredential();
                                            ((NetworkCredential)smtpClient.Credentials).UserName = Functions.FromBase64(command.Substring(0, command.Length - 2));

                                            await Functions.SendStreamStringAsync(clientStreamWriter, "334 UGFzc3dvcmQ6\r\n");
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 334 UGFzc3dvcmQ6", Proxy.LogLevel.Raw, LogLevel);
                                        }
                                        else
                                        {
                                            // Handle the password.
                                            inLoginAuth = false;
                                            ((NetworkCredential)smtpClient.Credentials).Password = Functions.FromBase64(command.Substring(0, command.Length - 2));

                                            await Functions.SendStreamStringAsync(clientStreamWriter, "235 OK\r\n");
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 235 OK", Proxy.LogLevel.Raw, LogLevel);
                                        }
                                        command = "";
                                    }
                                    else
                                    {
                                        // Otherwise, look at the verb of the incoming command.
                                        string[] commandParts = command.Substring(0, command.Length - 2).Replace("\r", "").Split(new char[] { ' ' }, 2);

                                        if (LogLevel == Proxy.LogLevel.Verbose)
                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Command {" + commandParts[0] + "} received.", Proxy.LogLevel.Verbose, LogLevel);

                                        switch (commandParts[0].ToUpper())
                                        {
                                            case "AUTH":
                                                // Support authentication.
                                                if (commandParts.Length > 1)
                                                {
                                                    commandParts = command.Substring(0, command.Length - 2).Replace("\r", "").Split(new char[] { ' ' });
                                                    switch (commandParts[1].ToUpper())
                                                    {
                                                        case "PLAIN":
                                                            // Prepare to handle a continuation command.
                                                            inPlainAuth = true;
                                                            await Functions.SendStreamStringAsync(clientStreamWriter, "334 Proceed\r\n");
                                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 334 Proceed", Proxy.LogLevel.Raw, LogLevel);

                                                            break;
                                                        case "LOGIN":
                                                            inLoginAuth = true;
                                                            if (commandParts.Length > 2)
                                                            {
                                                                // Parse the username and request a password.
                                                                smtpClient.Credentials = new NetworkCredential();
                                                                ((NetworkCredential)smtpClient.Credentials).UserName = Functions.FromBase64(commandParts[2]);

                                                                await Functions.SendStreamStringAsync(clientStreamWriter, "334 UGFzc3dvcmQ6\r\n");
                                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 334 UGFzc3dvcmQ6", Proxy.LogLevel.Raw, LogLevel);
                                                            }
                                                            else
                                                            {
                                                                // Request a username only.
                                                                await Functions.SendStreamStringAsync(clientStreamWriter, "334 VXNlcm5hbWU6\r\n");
                                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 334 VXNlcm5hbWU6", Proxy.LogLevel.Raw, LogLevel);
                                                            }
                                                            break;
                                                        default:
                                                            // Split up an AUTH PLAIN handshake into its components.
                                                            string authString = Encoding.UTF8.GetString(Convert.FromBase64String(commandParts[1].Substring(6)));
                                                            string[] authStringParts = authString.Split(new char[] { '\0' }, 3);
                                                            if (authStringParts.Length > 2 && arguments.RemoteServerCredential == null)
                                                                smtpClient.Credentials = new NetworkCredential(authStringParts[1], authStringParts[2]);

                                                            await Functions.SendStreamStringAsync(clientStreamWriter, "235 OK\r\n");
                                                            ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 235 OK", Proxy.LogLevel.Raw, LogLevel);
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    await Functions.SendStreamStringAsync(clientStreamWriter, "500 Unknown verb\r\n");
                                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 500 Unknown verb", Proxy.LogLevel.Raw, LogLevel);
                                                }
                                                break;
                                            case "DATA":
                                                // Prepare to handle continuation data.
                                                sending = true;
                                                command = command.Substring(6);
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "354 Send message content; end with <CRLF>.<CRLF>\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 354 Send message content; end with <CRLF>.<CRLF>", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "EHLO":
                                                // Proceed with the login and send a list of supported commands.
                                                if (commandParts.Length > 1)
                                                    identity = commandParts[1] + " ";
                                                if (arguments.LocalEnableSsl)
                                                {
                                                    await Functions.SendStreamStringAsync(clientStreamWriter, "250-Hello " + identity + "[" + ip + "], please proceed\r\n250-AUTH LOGIN PLAIN\r\n250-RSET\r\n250 STARTTLS\r\n");
                                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 250-Hello " + identity + "[" + ip + "], please proceed\r\n250-AUTH LOGIN PLAIN\r\n250-RSET\r\n250 STARTTLS", Proxy.LogLevel.Raw, LogLevel);
                                                }
                                                else
                                                {
                                                    await Functions.SendStreamStringAsync(clientStreamWriter, "250-Hello " + identity + "[" + ip + "], please proceed\r\n250-AUTH LOGIN PLAIN\r\n250 RSET\r\n");
                                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 250-Hello " + identity + "[" + ip + "], please proceed\r\n250-AUTH LOGIN PLAIN\r\n250 RSET", Proxy.LogLevel.Raw, LogLevel);
                                                }
                                                break;
                                            case "HELO":
                                                // Proceed with the login.
                                                if (commandParts.Length > 1)
                                                    identity = commandParts[1];
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "250 Hello " + identity + " [" + ip + "], please proceed\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: Hello " + identity + " [" + ip + "], please proceed", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "MAIL":
                                            case "SAML":
                                            case "SEND":
                                            case "SOML":
                                                // Accept the from address.
                                                if (commandParts.Length > 1 && commandParts[1].Length > 5)
                                                    fromAddress = commandParts[1].Substring(5);
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "250 OK\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 250 OK", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "NOOP":
                                                // Prolong the current session.
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "250 Still here\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 250 Still here", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "PASS":
                                                // Support authentication.
                                                if (commandParts.Length > 1 && arguments.RemoteServerCredential == null)
                                                    ((NetworkCredential)smtpClient.Credentials).Password = commandParts[1];
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "235 OK\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 235 OK", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "QUIT":
                                                // Wait one second then force the current connection closed.
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "221 Bye\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 221 Bye", Proxy.LogLevel.Raw, LogLevel);

                                                Thread.Sleep(1000);

                                                if (clientStream != null)
                                                    clientStream.Dispose();
                                                if (client != null)
                                                    client.Close();
                                                break;
                                            case "RCPT":
                                                // Acknolwedge recipients.
                                                if (commandParts.Length > 1 && commandParts[1].Length > 6)
                                                {
                                                    //toList.Add(commandParts[1].Substring(5, commandParts[1].Length - 6)); //toList原本是List<string>，有些軟體送出來的TO:<xxx@xxx.xxx>會抓錯
                                                    toList.Add(MailboxAddress.Parse(commandParts[1].Remove(0, 3)));
                                                }
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "250 OK\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 250 OK", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "RSET":
                                                // Reset the current message arguments.
                                                fromAddress = "";
                                                toList.Clear();

                                                await Functions.SendStreamStringAsync(clientStreamWriter, "250 OK\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 250 OK", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "STARTTLS":
                                                // If supported, upgrade the session's security through a TLS handshake.
                                                if (arguments.LocalEnableSsl)
                                                {
                                                    await Functions.SendStreamStringAsync(clientStreamWriter, "220 Go ahead\r\n");
                                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 220 Go ahead", Proxy.LogLevel.Raw, LogLevel);

                                                    if (!(clientStream is SslStream))
                                                    {
                                                        clientStream = new SslStream(clientStream);
                                                        ((SslStream)clientStream).AuthenticateAsServer(arguments.Certificate);

                                                        clientStreamReader = new StreamReader(clientStream);
                                                        clientStreamWriter = new StreamWriter(clientStream);
                                                        clientStreamWriter.AutoFlush = true;
                                                    }
                                                }
                                                else
                                                {
                                                    await Functions.SendStreamStringAsync(clientStreamWriter, "500 Unknown verb\r\n");
                                                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 500 Unknown verb", Proxy.LogLevel.Raw, LogLevel);
                                                }
                                                break;
                                            case "USER":
                                                // Support authentication.
                                                if (commandParts.Length > 1 && arguments.RemoteServerCredential == null)
                                                    ((NetworkCredential)smtpClient.Credentials).UserName = commandParts[1];

                                                await Functions.SendStreamStringAsync(clientStreamWriter, "235 OK\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 235 OK", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            case "VRFY":
                                                // Notify that we can't verify addresses.
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "252 I'm just a proxy\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 252 I'm just a proxy", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                            default:
                                                await Functions.SendStreamStringAsync(clientStreamWriter, "500 Unknown verb\r\n");
                                                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "S: 500 Unknown verb", Proxy.LogLevel.Raw, LogLevel);
                                                break;
                                        }

                                        command = "";
                                    }
                                }
                            }
                        }
                        else
                            stillReceiving = false;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore either stream being closed.
            }
            catch (SocketException ex)
            {
                if (arguments.DebugMode || System.Diagnostics.Debugger.IsAttached)
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Exception communicating with {" + arguments.RemoteServerHostName + "} on port {" + arguments.RemoteServerPort + "}: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                else
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Exception communicating with {" + arguments.RemoteServerHostName + "} on port {" + arguments.RemoteServerPort + "}: " + ex.Message, Proxy.LogLevel.Error, LogLevel);
            }
            catch (Exception ex)
            {
                if (arguments.DebugMode || System.Diagnostics.Debugger.IsAttached)
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Exception: " + ex.ToString(), Proxy.LogLevel.Error, LogLevel);
                else
                    ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, "Exception: " + ex.Message, Proxy.LogLevel.Error, LogLevel);
            }
            finally
            {
                ProxyFunctions.Log(LogWriter, SessionId, arguments.ConnectionId, $"SMTP Connection closed after transmitting {FFunc.GetSizeString(bytesTransmitted)}.", Proxy.LogLevel.Information, LogLevel);

                // Clean up after any unexpectedly closed connections.
                if (clientStreamWriter != null)
                    clientStreamWriter.Dispose();
                if (clientStreamReader != null)
                    clientStreamReader.Dispose();
                if (clientStream != null)
                    clientStream.Dispose();
                if (client != null)
                    client.Close();
            }
        }

        /// <summary>
        /// Start an individual SMTP proxy on its own thread.
        /// </summary>
        /// <param name="parameters">SmtpProxyArguments object containing all parameters for this connection.</param>
        private static void StartProxy(object parameters)
        {
            SmtpProxyArguments arguments = (SmtpProxyArguments)parameters;

            // Start the proxy using passed-in settings.
            arguments.Proxy.Start(arguments.AcceptedIPs, arguments.LocalIpAddress, arguments.LocalPort, arguments.LocalEnableSsl, arguments.RemoteServerHostName, arguments.RemoteServerPort, arguments.RemoteServerEnableSsl, arguments.RemoteServerCredential, arguments.FixedFrom, arguments.FixedTo, arguments.FixedCC, arguments.FixedBcc, arguments.FixedSignature, arguments.SmimeSettingsMode, arguments.SmimeSigned, arguments.SmimeEncryptedEnvelope, arguments.SmimeTripleWrapped, arguments.SmimeRemovePreviousOperations, arguments.SendCertificateReminders, arguments.ExportDirectory, arguments.LogFile, arguments.LogLevel, arguments.InstanceId, arguments.SmimeValidCertificates, arguments.DebugMode, arguments.EMailAddr, arguments.WebService, arguments.LogWriteEnable, arguments.radioClient);
        }


        /// <summary>
        /// Get address list with "," from InternetAddressList  in Mailkit message
        /// </summary>
        ///
        private static string GetAddressListfromMessage(InternetAddressList addrList)
        {
            List<string> Emails = new List<string>();
            foreach (MailboxAddress toAddr in addrList)
            {
                Emails.Add(toAddr.Address);
            }
            return string.Join(",", Emails);
        }


        #endregion Private Methods
    }
}