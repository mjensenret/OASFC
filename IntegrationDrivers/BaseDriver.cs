using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IntegrationDrivers
{
    public abstract class BaseDriver
    {
        private static string deviceIpAddress;
        private static string devicePort;
        private bool isConnected = false;
        private string volumeUnits;
        public Socket sock;

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

        //public BaseDriver()
        //{

        //}

        //public BaseDriver(string ipAddress, string port)
        //{
        //    deviceIpAddress = ipAddress;
        //    devicePort = port;
        //    sock = socket();
        //}

        //public virtual void Connect(string ipAddress, string port)
        //{
        //    sock = socket();
        //    Console.WriteLine("Connect");
        //    try
        //    {

        //        if (!(isConnected))
        //        {
        //            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), Convert.ToInt32(port));

        //            try
        //            {
        //                sock.Connect(endPoint);
        //            }
        //            catch (ObjectDisposedException od)
        //            {
        //                sock = socket();
        //                sock.Connect(endPoint);

        //            }
        //            catch (Exception e)
        //            {

        //            }

        //        }
        //        isConnected = true;

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("{0} : {1}", "Connect", ex.Message);
        //        //m_OASDriverInterface.UpdateSystemError(true, "Connect", 1, "GetDefaultDriverConfig Exception: " + ex.Message);

        //    }
        //}

        //public bool Disconnect()
        //{
        //    Console.WriteLine("Disconnect");
        //    try
        //    {
        //        if (!(isConnected))
        //            return isConnected;

        //        // shut down polling timer
        //        //localTimer.Change(Timeout.Infinite, Timeout.Infinite);

        //        //SendCommand("DA");
        //        sock.Close();

        //        isConnected = false;
        //        return isConnected;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("{0} : {1}", "Disconnect", ex.Message);
        //        //m_OASDriverInterface.UpdateSystemError(true, "Disconnect", 1, "GetDefaultDriverConfig Exception: " + ex.Message);
        //        return false;
        //    }
        //}

        //Socket socket()
        //{
        //    return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //}

        //public double volumeConvert(byte[] volumeBytes, int size)
        //{
        //    Array.Resize(ref volumeBytes, size);

        //    double volume = Convert.ToDouble(Encoding.ASCII.GetString(volumeBytes, 15, 8));

        //    if (volumeUnits == "Gallons")
        //        volume = volume / 42;

        //    return volume;
        //}
    }
}
