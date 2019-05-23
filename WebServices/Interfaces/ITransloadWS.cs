using System;
using System.Collections.Generic;
using System.Text;

namespace WebServices.Interfaces
{
    public interface ITransloadWS
    {
        void Connect();
        bool CheckAuditNumber(int transferOrderId);
    }
}
