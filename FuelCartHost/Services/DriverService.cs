using Common.Models;
using FuelCartHost.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using UniversalDriverInterface;
using WebServices.Interfaces;

namespace FuelCartHost.Services
{
    class DriverService : IDriverService
    {
        private readonly UDISettings _config;
        private readonly RegisterHeadSettings _registerHead;
        private static DriverInterface m_Driver;
        private readonly ILogger<DriverService> log;
        private readonly ITransloadWS _transloadWS;

        public DriverService(IOptions<UDISettings> config, IOptions<RegisterHeadSettings> registerHead, ILogger<DriverService> logger, ITransloadWS transloadWS)
        {
            _config = config.Value;
            _registerHead = registerHead.Value;
            log = logger;
            _transloadWS = transloadWS;

        }

        public void StartDriverInterface()
        {
            //Console.WriteLine("Starting driver interface.");
            log.LogInformation("StartDriverInterface has been called.");

            //m_Driver = new DriverInterface(_config.ServiceNode, _config.LiveDataCloudNode, _config.PortNumber, _config.MachineName, _config.StoreAndForward, _config.Username, _config.Password, _registerHead.DeviceType, _registerHead);
            m_Driver = new DriverInterface(_config, _registerHead, _transloadWS);
        }

        public void StopDriverInterface()
        {
            if(m_Driver != null)
            {
                m_Driver.Disconnect();
                m_Driver = null;
            }
        }
    }
}
