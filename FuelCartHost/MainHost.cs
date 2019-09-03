using FuelCartHost.Interfaces;
using FuelCartHost.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniversalDriverInterface;
using WebServices.Interfaces;
using Common.Models;

namespace FuelCartHost
{
    public class MainHost
    {
        private readonly ILogger<MainHost> _logger;
        private readonly AppSettingsModel _appSettings;
        private readonly UDISettings _uDISettings;
        private readonly RegisterHeadSettings _registerHeadSettings;
        private readonly IDriverService _driverService;
        private readonly ITransloadWS _transloadWS;

        private static DriverInterface m_Driver;

        private AppDomain _dom;

        private AppDomain dom
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

        public MainHost(ILogger<MainHost> logger, IDriverService driverService, ITransloadWS transloadWS, IOptions<UDISettings> udiSettings, IOptions<RegisterHeadSettings> registerHeadSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            //_appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
            _driverService = driverService;
            _transloadWS = transloadWS;
            _uDISettings = udiSettings.Value;
            _registerHeadSettings = registerHeadSettings.Value;
            dom = System.AppDomain.CurrentDomain;
        }

        public void Run()
        {
            _logger.LogInformation("Application starting....");
            _driverService.StartDriverInterface();
            _transloadWS.Connect();

            ConsoleHost.WaitForShutdown();
        }

        private void doCleanup()
        {
            _driverService.StopDriverInterface();
        }

        public void ProcessExit(object sender, EventArgs e)
        {
            doCleanup();
        }

    }
}
