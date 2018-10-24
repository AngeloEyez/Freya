using System;
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
            if (Environment.UserInteractive)
            {
                ProxyService myServ = new ProxyService();
                myServ.TestStartupAndStop(args);
            }

            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new ProxyService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
