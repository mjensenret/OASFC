using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models
{
    public class RegisterHeadSettings
    {
        public int CartId { get; set; }
        public string DeviceType { get; set; }
        public string IPAddress { get; set; }
        public string ArmAddress { get; set; }
        public string Port { get; set; }
        public string VolumeUnits { get; set; }
    }
}
