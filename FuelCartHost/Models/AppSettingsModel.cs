using System;
using System.Collections.Generic;
using System.Text;

namespace FuelCartHost.Models
{
    public class AppSettingsModel
    {
        public string ServiceNode { get; set; }
        public int OASPortNumber { get; set; }
        public string MachineName { get; set; }
        public bool StoreAndForward { get; set; }
        public string OASUsername { get; set; }
        public string OASPassword { get; set; }
        public int CartId { get; set; }
        public string DeviceType { get; set; }
        public string IPAddress { get; set; }
        public string ArmAddress { get; set; }
        public string Port { get; set; }
        public string WebServiceURL { get; set; }
        public string WSUserName { get; set; }
        public string WSPassword { get; set; }
    }
}
