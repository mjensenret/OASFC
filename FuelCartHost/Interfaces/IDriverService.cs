using System;
using System.Collections.Generic;
using System.Text;
using UniversalDriverInterface;

namespace FuelCartHost.Interfaces
{
    public interface IDriverService
    {
        void StartDriverInterface();
        void StopDriverInterface();
    }
}
