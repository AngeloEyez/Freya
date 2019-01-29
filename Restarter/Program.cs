using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Restarter
{
    class Program
    {
        ///Restarter ServiceName pathOfFreya windowsState
        static void Main(string[] args)
        {
            //訂閱載入內崁DLL資源
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            if (args != null && args.Length > 0)
            {

                int count = 0;
                bool srvexist = ServiceExists(args[0]);
                
                while (!srvexist && count < 20)
                {
                    count++;
                    Console.WriteLine(count + ":" + srvexist);
                    Thread.Sleep(1000);
                    srvexist = ServiceExists(args[0]);
                } 

                do
                {
                    ServiceControllerStatus srvsatus = ServiceControllerStatus.Stopped;
                    try
                    {
                        ServiceController serviceContoller = new ServiceController(args[0]);
                        srvsatus = serviceContoller.Status;
                    }
                    catch { }
                    Console.WriteLine(count + ":" + srvsatus);
                    if (srvsatus == ServiceControllerStatus.Running)
                    {
                        Process p = new Process();
                        p.StartInfo.Arguments = args[2];
                        p.StartInfo.FileName = args[1];
                        p.Start();
                        break;
                    }
                    Thread.Sleep(1000);
                }
                while (true);

            }

        }


        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string resourceName = "Restarter." + new AssemblyName(args.Name).Name + ".dll";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }

        /// <summary>
        /// Confirm the Windows service exists.
        /// </summary>
        /// <param name="serviceName">Name of the WIndows service</param>
        private static bool ServiceExists(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            return services.FirstOrDefault(s => s.ServiceName == serviceName) != null;
        }
    }
}
