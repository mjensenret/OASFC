using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models
{
    public class UDISettings
    {
        public string ServiceNode { get; set; }
        public string LiveDataCloudNode { get; set; }
        public int PortNumber { get; set; }
        public string MachineName { get; set; }
        public bool StoreAndForward { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
