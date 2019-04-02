using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UniversalDriverInterface;

namespace FuelCartHost
{
    static class IntegrationHost
    {
        static IntegrationHost()
        {
            dom = System.AppDomain.CurrentDomain;
        }

        private static DriverInterface m_Driver;
        private static string ServiceNode = "127.0.0.1"; // IP Address, network node, or registered domain name when the central service is running.
        private static string LiveDataCloudNode = ""; // The live data cloud source service to use when the central service is hosting live data cloud nodes.
        private static Int32 PortNumber = 58727; // Port number to communicate to OAS Service.
        private static string MachineName = "LocalMachineName"; // Must be unique for each instance of the driver running.
        private static bool StoreAndForward = true; // When enabled data will be buffered on network failure or when the service is not running.
        private static string UserName = ""; // Required if using automatic tag creation on service with security enabled.
        private static string Password = ""; // Required if using automatic tag creation on service with security enabled.

        private static AppDomain _dom;

        private static AppDomain dom
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _dom;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_dom != null)
                {
                    _dom.ProcessExit -= ProcessExit;
                }

                _dom = value;
                if (_dom != null)
                {
                    _dom.ProcessExit += ProcessExit;
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                var config = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", false, true)
                   .Build();

                try
                {
                    ServiceNode = config["OASDriverConfig:ServiceNode"];
                    LiveDataCloudNode = config["OASDriverConfig:LiveDataCloudNode"];
                    PortNumber = int.Parse(config["OASDriverConfig:PortNumber"]);
                    MachineName = config["OASDriverConfig:MachineName"];
                    StoreAndForward = bool.Parse(config["OASDriverConfig:StoreAndForward"]);
                    UserName = config["OASDriverConfig:UserName"];
                    Password = config["OASDriverConfig:Password"];
                }
                catch (Exception)
                {
                }

                Console.WriteLine("Connecting...");
                // create instance of UDI
                m_Driver = new DriverInterface(ServiceNode, LiveDataCloudNode, PortNumber, MachineName, StoreAndForward, UserName, Password);

                ConsoleHost.WaitForShutdown();
            }
            catch (Exception ex)
            {
                doCleanup();
            }
        }

        public static void doCleanup()
        {
            if (m_Driver != null)
            {
                // allow for cleanup if any exceptions are thrown on start, or if the process shuts down, etc.
                m_Driver.Disconnect();
                m_Driver = null;
            }
        }
        public static void ProcessExit(object sender, EventArgs e)
        {
            doCleanup();
        }
    }
}
