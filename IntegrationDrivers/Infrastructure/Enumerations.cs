using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDrivers.Infrastructure
{
    public class Enumerations
    {
        public enum VolumeUnits
        {
            Gallons = 0,
            Barrels = 1,
            CubicMeters = 2
        }

        public enum CommandTypes
        {
            Status = 0,
            IVVolume = 1,
            GVVolume = 2,
            GSTVolume = 3,
            GSVVolume = 4,
            TransactionNumber = 5
        }

        public enum DataPoint
        {
            Status = 0,
            Prompt = 1,
            TransactionNum = 2,
            Time = 3,
            Volume = 4,
            Totalizer = 5,
            TransactionAvgs = 6,
            Configuration = 7
        }

        public enum MeasurementType
        {
            IV = 0,
            Gross = 1,
            GST = 2,
            GSV = 3,
            Mass = 4
        }
    }
}
