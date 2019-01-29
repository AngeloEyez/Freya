﻿/*
 * OpaqueMail (https://opaquemail.org/).
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

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using OpaqueMail;
using ZetaIpc.Runtime.Client;

namespace Freya.Proxy
{
    /// <summary>
    /// Arguments passed in when instantiating a new IMAP proxy instance.
    /// </summary>
    public struct ImapProxyArguments
    {
        public bool LogWriteEnable;
        public IpcClient radioClient;

        /// <summary>IP addresses to accept connections from.</summary>
        public string AcceptedIPs;
        /// <summary>Certificate to authenticate the server.</summary>
        public X509Certificate Certificate;
        /// <summary>Local IP address to bind to.</summary>
        public IPAddress LocalIpAddress;
        /// <summary>Local IP address to listen on.</summary>
        public int LocalPort;
        /// <summary>Whether the local server supports TLS/SSL.</summary>
        public bool LocalEnableSsl;
        /// <summary>Remote server hostname to forward all IMAP messages to.</summary>
        public string RemoteServerHostName;
        /// <summary>Remote server port to connect to.</summary>
        public int RemoteServerPort;
        /// <summary>Whether the remote IMAP server requires TLS/SSL.</summary>
        public bool RemoteServerEnableSsl;
        /// <summary>(Optional) Credentials to be used for all connections to the remote IMAP server.  When set, this overrides any credentials passed locally.</summary>
        public NetworkCredential RemoteServerCredential;
        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;

        /// <summary>The file where events and exception information should be logged.</summary>
        public string LogFile;
        /// <summary>Proxy logging level, determining how much information is logged.</summary>
        public LogLevel LogLevel;

        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;

        /// <summary>IMAP Proxy to start.</summary>
        public ImapProxy Proxy;
    }

    /// <summary>
    /// Arguments passed in when instantiating a new IMAP proxy connection instance.
    /// </summary>
    public struct ImapProxyConnectionArguments
    {
        /// <summary>IP addresses to accept connections from.</summary>
        public string AcceptedIPs;
        /// <summary>TCP connection to the client.</summary>
        public TcpClient TcpClient;
        /// <summary>Certificate to authenticate the server.</summary>
        public X509Certificate Certificate;
        /// <summary>Local IP address to bind to.</summary>
        public IPAddress LocalIpAddress;
        /// <summary>Local IP address to listen on.</summary>
        public int LocalPort;
        /// <summary>Whether the local server supports TLS/SSL.</summary>
        public bool LocalEnableSsl;
        /// <summary>Remote server hostname to forward all IMAP messages to.</summary>
        public string RemoteServerHostName;
        /// <summary>Remote server port to connect to.</summary>
        public int RemoteServerPort;
        /// <summary>Whether the remote IMAP server requires TLS/SSL.</summary>
        public bool RemoteServerEnableSsl;
        /// <summary>(Optional) Credentials to be used for all connections to the remote IMAP server.  When set, this overrides any credentials passed locally.</summary>
        public NetworkCredential RemoteServerCredential;
        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;

        /// <summary>A unique connection identifier for logging.</summary>
        public string ConnectionId;
        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;
    }

    /// <summary>
    /// Proxy logging level, determining how much information is logged.
    /// </summary>
    public enum LogLevel
    {
        None = 0,           // No logging.
        Critical = 1,       // Only critical errors, such as the inability to start a proxy.
        Error = 2,          // All errors, including unexpected disconnections.
        Warning = 4,        // Errors and warnings, such as rejected IPs.
        Information = 8,    // Errors, warnings, and other information, such as starting and stopping messages.
        Verbose = 16,       // All of the above, plus individual commands.
        Raw = 32            // All of the above, plus the raw client and server bytes.
                                // Warning: sensitive information such as credentials will be saved with Raw logging.
    }

    /// <summary>
    /// Arguments passed in when instantiating a new POP3 proxy instance.
    /// </summary>
    public struct Pop3ProxyArguments
    {
        /// <summary>IP addresses to accept connections from.</summary>
        public string AcceptedIPs;
        /// <summary>Certificate to authenticate the server.</summary>
        public X509Certificate Certificate;
        /// <summary>Local IP address to bind to.</summary>
        public IPAddress LocalIpAddress;
        /// <summary>Local IP address to listen on.</summary>
        public int LocalPort;
        /// <summary>Whether the local server supports TLS/SSL.</summary>
        public bool LocalEnableSsl;
        /// <summary>Remote server hostname to forward all POP3 messages to.</summary>
        public string RemoteServerHostName;
        /// <summary>Remote server port to connect to.</summary>
        public int RemoteServerPort;
        /// <summary>Whether the remote POP3 server requires TLS/SSL.</summary>
        public bool RemoteServerEnableSsl;
        /// <summary>(Optional) Credentials to be used for all connections to the remote POP3 server.  When set, this overrides any credentials passed locally.</summary>
        public NetworkCredential RemoteServerCredential;
        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;

        /// <summary>The file where events and exception information should be logged.</summary>
        public string LogFile;
        /// <summary>Proxy logging level, determining how much information is logged.</summary>
        public LogLevel LogLevel;

        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;
        
        /// <summary>POP3 Proxy to start.</summary>
        public Pop3Proxy Proxy;
    }

    /// <summary>
    /// Arguments passed in when instantiating a new POP3 proxy connection instance.
    /// </summary>
    public struct Pop3ProxyConnectionArguments
    {
        /// <summary>IP addresses to accept connections from.</summary>
        public string AcceptedIPs;
        /// <summary>TCP connection to the client.</summary>
        public TcpClient TcpClient;
        /// <summary>Certificate to authenticate the server.</summary>
        public X509Certificate Certificate;
        /// <summary>Local IP address to bind to.</summary>
        public IPAddress LocalIpAddress;
        /// <summary>Local IP address to listen on.</summary>
        public int LocalPort;
        /// <summary>Whether the local server supports TLS/SSL.</summary>
        public bool LocalEnableSsl;
        /// <summary>Remote server hostname to forward all POP3 messages to.</summary>
        public string RemoteServerHostName;
        /// <summary>Remote server port to connect to.</summary>
        public int RemoteServerPort;
        /// <summary>Whether the remote POP3 server requires TLS/SSL.</summary>
        public bool RemoteServerEnableSsl;
        /// <summary>(Optional) Credentials to be used for all connections to the remote POP3 server.  When set, this overrides any credentials passed locally.</summary>
        public NetworkCredential RemoteServerCredential;
        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;
        
        /// <summary>A unique connection identifier for logging.</summary>
        public string ConnectionId;
        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;
    }
    
    /// <summary>
    /// Arguments passed when processing a message.
    /// </summary>
    public struct ProcessMessageArguments
    {
        /// <summary>The text of the message to process.</summary>
        public string MessageText;

        /// <summary>A unique connection identifier for logging.</summary>
        public string ConnectionId;

        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;

        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;
        /// <summary>The user transmitting this message.</summary>
        public string UserName;
    }

    /// <summary>
    /// Arguments passed in when instantiating a new SMTP proxy instance.
    /// </summary>
    public struct SmtpProxyArguments
    {
        /// <summary>WebService IP for SuperNote</summary>
        public string WebService;
        /// <summary>Email address for SuperNote</summary>
        public string EMailAddr;
        public bool LogWriteEnable;
        public IpcClient radioClient;

        /// <summary>IP addresses to accept connections from.</summary>
        public string AcceptedIPs;
        /// <summary>Certificate to authenticate the server.</summary>
        public X509Certificate Certificate;
        /// <summary>Local IP address to bind to.</summary>
        public IPAddress LocalIpAddress;
        /// <summary>Local IP address to listen on.</summary>
        public int LocalPort;
        /// <summary>Whether the local server supports TLS/SSL.</summary>
        public bool LocalEnableSsl;
        /// <summary>Remote server hostname to forward all SMTP messages to.</summary>
        public string RemoteServerHostName;
        /// <summary>Remote server port to connect to.</summary>
        public int RemoteServerPort;
        /// <summary>Whether the remote SMTP server requires TLS/SSL.</summary>
        public bool RemoteServerEnableSsl;
        /// <summary>(Optional) Credentials to be used for all connections to the remote SMTP server.  When set, this overrides any credentials passed locally.</summary>
        public NetworkCredential RemoteServerCredential;
        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;
        /// <summary>(Optional) "From" address for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedFrom;
        /// <summary>(Optional) "To" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedTo;
        /// <summary>(Optional) "CC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedCC;
        /// <summary>(Optional) "BCC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedBcc;
        /// <summary>(Optional) Signature to add to the end of each sent message.</summary>
        public string FixedSignature;

        /// <summary>Encrypt the email's envelope.  When SmimeSign is true, encryption is the second S/MIME operation.</summary>
        public bool SmimeEncryptedEnvelope;
        /// <summary>Remove envelope encryption and signatures from passed-in messages.  If true and SmimeSigned or SmimeEncryptEnvelope is also true, new S/MIME operations will be applied.</summary>
        public bool SmimeRemovePreviousOperations;
        /// <summary>Whether S/MIME settings for encryption and signing are explicitly required or only preferred.</summary>
        public SmimeSettingsMode SmimeSettingsMode;
        /// <summary>Sign the email.  When true, signing is the first S/MIME operation.</summary>
        public bool SmimeSigned;
        /// <summary>Triple-wrap the email by signing, then encrypting the envelope, then signing the encrypted envelope.</summary>
        public bool SmimeTripleWrapped;

        /// <summary>Collection of certificates to be used when searching for recipient public keys.</summary>
        /// <remarks>If not specified, SmtpClient will use the current Windows user's certificate store.</remarks>
        public X509Certificate2Collection SmimeValidCertificates;

        /// <summary>The file where events and exception information should be logged.</summary>
        public string LogFile;
        /// <summary>Proxy logging level, determining how much information is logged.</summary>
        public LogLevel LogLevel;

        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;

        /// <param name="sendCertificateReminders">Send email reminders when a signing certificate is due to expire within 30 days.</param>
        public bool SendCertificateReminders;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;
        
        /// <summary>SMTP Proxy to start.</summary>
        public SmtpProxy Proxy;

    }

    /// <summary>
    /// Arguments passed in when instantiating a new SMTP proxy connection instance.
    /// </summary>
    public struct SmtpProxyConnectionArguments
    {
        /// <summary>WebService IP for SuperNote</summary>
        public string WebService;
        /// <summary>Email address for SuperNote</summary>
        public string EMailAddr;


        /// <summary>IP addresses to accept connections from.</summary>
        public string AcceptedIPs;
        /// <summary>TCP connection to the client.</summary>
        public TcpClient TcpClient;
        /// <summary>Certificate to authenticate the server.</summary>
        public X509Certificate Certificate;
        /// <summary>Local IP address to bind to.</summary>
        public IPAddress LocalIpAddress;
        /// <summary>Local IP address to listen on.</summary>
        public int LocalPort;
        /// <summary>Whether the local server supports TLS/SSL.</summary>
        public bool LocalEnableSsl;
        /// <summary>Remote server hostname to forward all SMTP messages to.</summary>
        public string RemoteServerHostName;
        /// <summary>Remote server port to connect to.</summary>
        public int RemoteServerPort;
        /// <summary>Whether the remote SMTP server requires TLS/SSL.</summary>
        public bool RemoteServerEnableSsl;
        /// <summary>(Optional) Credentials to be used for all connections to the remote SMTP server.  When set, this overrides any credentials passed locally.</summary>
        public NetworkCredential RemoteServerCredential;
        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;
        /// <summary>(Optional) "From" address for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedFrom;
        /// <summary>(Optional) "To" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedTo;
        /// <summary>(Optional) "CC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedCC;
        /// <summary>(Optional) "BCC" address(es) for all sent messages.  When supplied, it will override any values sent from the client.</summary>
        public string FixedBcc;
        /// <summary>(Optional) Signature to add to the end of each sent message.</summary>
        public string FixedSignature;

        /// <summary>Encrypt the email's envelope.  When SmimeSign is true, encryption is the second S/MIME operation.</summary>
        public bool SmimeEncryptedEnvelope;
        /// <summary>Whether S/MIME settings for encryption and signing are explicitly required or only preferred.</summary>
        public SmimeSettingsMode SmimeSettingsMode;
        /// <summary>Remove envelope encryption and signatures from passed-in messages.  If true and SmimeSigned or SmimeEncryptEnvelope is also true, new S/MIME operations will be applied.</summary>
        public bool SmimeRemovePreviousOperations;
        /// <summary>Sign the email.  When true, signing is the first S/MIME operation.</summary>
        public bool SmimeSigned;
        /// <summary>Triple-wrap the email by signing, then encrypting the envelope, then signing the encrypted envelope.</summary>
        public bool SmimeTripleWrapped;

        /// <summary>Collection of certificates to be used when searching for recipient public keys.</summary>
        /// <remarks>If not specified, SmtpClient will use the current Windows user's certificate store.</remarks>
        public X509Certificate2Collection SmimeValidCertificates;

        /// <param name="sendCertificateReminders">Send email reminders when a signing certificate is due to expire within 30 days.</param>
        public bool SendCertificateReminders;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;
        
        /// <summary>A unique connection identifier for logging.</summary>
        public string ConnectionId;
        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;
    }

    /// <summary>
    /// Arguments passed when relaying commands between two connections.
    /// </summary>
    public struct TransmitArguments
    {
        /// <summary>Stream to read commands from.</summary>
        public Stream ClientStream;
        /// <summary>Stream to rebroadcast commands to.</summary>
        public Stream RemoteServerStream;

        /// <summary>Whether the target of this invocation is the client.</summary>
        public bool IsClient;

        /// <summary>A unique connection identifier for logging.</summary>
        public string ConnectionId;

        /// <summary>The instance number of the proxy.</summary>
        public int InstanceId;

        /// <summary>IP address of client.</summary>
        public string IPAddress;

        /// <summary>(Optional) Credentials to be used for all connections to the remote server.  When set, this overrides any credentials passed locally.</summary>
        public NetworkCredential Credential;

        /// <summary>(Optional) Location where all incoming messages are saved as EML files.</summary>
        public string ExportDirectory;

        /// <summary>Whether the proxy instance is running in DEBUG mode and should output full exception messages.</summary>
        public bool DebugMode;
    }
}