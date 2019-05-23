using Common.Models;
using FuelCartHost.Interfaces;
using FuelCartHost.Models;
using FuelCartHost.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UniversalDriverInterface;
using WebServices.Interfaces;
using WebServices;
using IntegrationDrivers;

namespace FuelCartHost
{
    static class IntegrationHost
    {
        static IntegrationHost()
        {
        }

        static void Main(string[] args)
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                serviceProvider.GetService<MainHost>().Run();
            }
            catch (Exception ex)
            {

            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // configure logging
            serviceCollection
                .AddLogging(b => b
                    .AddDebug()
                    .AddConsole()
                );

            // build config
            var config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false, true)
               .AddEnvironmentVariables()
               .Build();

            serviceCollection.AddOptions();
            serviceCollection.Configure<UDISettings>(config.GetSection("UDISettings"));
            serviceCollection.Configure<WebServiceSettings>(config.GetSection("WebServiceSettings"));
            serviceCollection.Configure<RegisterHeadSettings>(config.GetSection("RegisterHeadSettings"));

            // add services:
            serviceCollection.AddTransient<IDriverService, DriverService>();
            serviceCollection.AddTransient<ITransloadWS, TransloadWS>();

            // add app
            serviceCollection.AddTransient<MainHost>();
        }
    }
}
