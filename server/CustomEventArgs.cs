using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWPF
{
    // sarà utilizzato sia dal delegate ActivatedState che ConnectedState
    public class ClientConnectionEventArgs:EventArgs
    {
        public String ClientAddress { get; private set; }

        public ClientConnectionEventArgs(String clientAddress)
        {
            ClientAddress = clientAddress;
        }
        
    }

    public class MouseMoveEventArgs:EventArgs
    {
        public int CoordX { get; private set; }
    
        public int CoordY { get; private set; }

        public MouseMoveEventArgs(int x, int y)
        {
            CoordX = x;
            CoordY = y;
        }
    }

    public class MouseWheelEventArgs:EventArgs
    {
        public int Delta { get; private set; }

        public MouseWheelEventArgs(int deltaWheel){
            Delta = deltaWheel;
        }

    }

    public class KeyboardEventArgs : EventArgs
    {
        public byte CodKey { get; private set; }

        public KeyboardEventArgs(byte codKey)
        {
            CodKey = codKey;
        }
    }

    public class ClientErrorEventArgs : EventArgs
    {
        public const int CONNECTION_ERROR = 0;
        public const int NETWORK_ERROR = 1;
        public const int CLIENT_ERROR = 2;
        public const int GENERIC_ERROR = 3;


        public int ErrorCode { get; set; }
    }

}
