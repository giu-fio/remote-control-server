using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace ServerWPF
{
    public class Connection : IConnection
    {
        private ConnectionParameter connectionParameter;
        private IConnectionState state;
        private double widthClientMonitor;
        private double heightClientMonitor;
        public const int TIMEOUT = 25000;

        public Connection(ConnectionParameter param)
        {
            connectionParameter = param;
        }

        public double HeightClientMonitor
        {
            get { return heightClientMonitor; }
            set { heightClientMonitor = value; }
        }
        public double WidthClientMonitor
        {
            get { return widthClientMonitor; }
            set { widthClientMonitor = value; }
        }
        public IConnectionState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

        public void Start()
        {
            Trace.TraceInformation("Start");
            if (connectionParameter.UdpEnabled)
            {
                State = new UdpDisconnectedState(this, connectionParameter);
            }
            else
            {
                State = new TcpDisconnectedState(this, connectionParameter);
            }
        }
        // chiudo la connessione e metto State a Stop
        public void Stop()
        {
            if (State != null)
            {
                State.Stop();
            }
        }

        #region Events

        public event EventHandler<EventArgs> ActiveClient;

        public event EventHandler<ClientConnectionEventArgs> ConnectClient;

        public event EventHandler<EventArgs> DisconnectClient;

        public event EventHandler<EventArgs> CloseConnection;

        public event EventHandler<ClientErrorEventArgs> ClientError;
        
        #endregion

        #region Raise event methods

        internal void ActiveClientMessageRecived()
        {
            EventHandler<EventArgs> handler = ActiveClient;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        internal void ConnectClientMessageRecived(String clientAddress, uint widthMonitor, uint heightMonitor)
        {
            if (widthMonitor > 0)
            {
                widthClientMonitor = (int)widthMonitor;
            }
            if (heightMonitor > 0)
            {
                heightClientMonitor = (int)heightMonitor;
            }
            ClientConnectionEventArgs eventArgs = new ClientConnectionEventArgs(clientAddress);
            EventHandler<ClientConnectionEventArgs> handler = ConnectClient;
            if (handler != null)
            {
                handler(this, eventArgs);
            }
        }

        internal void DisconnectClientMessageRecived()
        {
            EventHandler<EventArgs> handler = DisconnectClient;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        internal void CloseConnectionMessageRecived()
        {
            EventHandler<EventArgs> handler = CloseConnection;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        internal void ClientErrorMessageReceived(ClientErrorEventArgs args)
        {
            EventHandler<ClientErrorEventArgs> handler = ClientError;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        #endregion

    }
}
