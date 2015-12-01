using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWPF
{

    public enum State
    {
        ACTIVE, CONNECTED, DISCONNECTED
    }
    public enum InnerState { ACTIVE, STOP_BY_USER, STOP_BY_ERROR, STOP_BY_NETWORK_ERROR, STOP_BY_CONTROLLER }

    public interface IConnectionState
    {
        void Stop();
    }
}
