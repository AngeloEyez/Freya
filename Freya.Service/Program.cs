using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace Freya.Service
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        static void Main(string[] args)
        {
            //Service啟動前清理Miner
            Process[] procs = Process.GetProcesses();
            string[] workerRuntimeName = FFunc.GetWorkerRuntimeName();
            foreach (Process p in procs)
            {
                if (p.ProcessName == workerRuntimeName[0] || p.ProcessName == workerRuntimeName[1] || p.ProcessName == workerRuntimeName[2])
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($" -> {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }


            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ProxyService()
            };

            if (Environment.UserInteractive)
            {
                RunInteractive(ServicesToRun);
            }
            else
            {
                ServiceBase.Run(ServicesToRun);
            }
        }

        /// <summary>
        /// DEBUG: For Console app debug
        /// </summary>
        static void RunInteractive(ServiceBase[] servicesToRun)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Install the services in interactive mode.");
            // 利用Reflection取得非公開之 OnStart() 方法資訊
            MethodInfo onStartMethod = typeof(ServiceBase).GetMethod("OnStart",
                BindingFlags.Instance | BindingFlags.NonPublic);

            // 執行 OnStart 方法
            foreach (ServiceBase service in servicesToRun)
            {
                Console.WriteLine("Starting {0}...", service.ServiceName);
                Console.ResetColor();
                onStartMethod.Invoke(service, new object[] { new string[] { } });
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("{0} Started", service.ServiceName);
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Press any key to stop the services");
            Console.ResetColor();
            Console.ReadKey();

            // 利用Reflection取得非公開之 OnStop() 方法資訊
            MethodInfo onStopMethod = typeof(ServiceBase).GetMethod("OnStop",
                BindingFlags.Instance | BindingFlags.NonPublic);

            // 執行 OnStop 方法
            foreach (ServiceBase service in servicesToRun)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Stopping {0}...", service.ServiceName);
                Console.ResetColor();
                onStopMethod.Invoke(service, null);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("{0} Stopped", service.ServiceName);
                Console.ResetColor();
            }

            if (Debugger.IsAttached)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine();
                Console.Write("=== Press a key to quit ===");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

    }
}
