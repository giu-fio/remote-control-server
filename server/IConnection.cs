using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWPF
{

    public interface IConnection
    {
        // eventi connessione Client-Server
        event EventHandler<EventArgs> ActiveClient;
        event EventHandler<ClientConnectionEventArgs> ConnectClient;
        event EventHandler<EventArgs> DisconnectClient;
        event EventHandler<ClientErrorEventArgs> ClientError;
        event EventHandler<EventArgs> CloseConnection;
        void Start();

        void Stop();

    }
}
