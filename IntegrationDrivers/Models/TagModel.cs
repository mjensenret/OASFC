using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDrivers.Models
{
    public class TagModel
    {
        public string TagName { get; set; }
        public double PollingRate { get; set; }
        public DateTime LastRead { get; set; }
        public object Value { get; set; }
        public string ValueDataType { get; set; }
        public string DataPoint { get; set; }
    }
}
        