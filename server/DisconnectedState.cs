using System;
using System.Text;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;

namespace ServerWPF
{
    public abstract class AbstractDisconnectedState : IConnectionState
    {
        private const int COMMAND_LENGHT = 6;
        private const int CHALLANGE_LENGHT = 4;
        private const short PORT_BROADCAST = 9999;
        private const short PORT_BROADCAST_CLIENT = 10000;
        private const string CONNECTED_RESULT = "CONNECTED_RESULT";
        private const string NETWORK_ERROR_RESULT = "NETWORK_ERROR_RESULT";
        private const string CLIENT_ERROR_RESULT = "CLIENT_ERROR_RESULT";
        private const string GENERIC_ERROR_RESULT = "GENERIC_ERROR_RESULT";
        private const string LOGIN_ERROR_RESULT = "ERROR_LOGIN_RESULT";
        private const string STOPPED_RESULT = "STOPPED_RESULT";

        private TcpListener listener;
        private BackgroundWorker controlConnectionListener;
        private BackgroundWorker workerBroadcast;
        private Socket socketBroadcast;
        private bool isStopped;
        private uint width;
        private uint height;
        protected ConnectionParameter connectionParameter;
        protected Connection connection;
        protected TcpClient tcpControlConnection;

        public AbstractDisconnectedState(Connection connection, ConnectionParameter param)
        {
            isStopped = false;
            this.connectionParameter = param;
            this.connection = connection;
            controlConnectionListener = new BackgroundWorker();
            controlConnectionListener.WorkerSupportsCancellation = true;
            controlConnectionListener.DoWork += controlConnectionListener_DoWork;
            controlConnectionListener.RunWorkerCompleted += controlConnectionListener_RunWorkerCompleted;
            workerBroadcast = new BackgroundWorker();
            workerBroadcast.DoWork += workerBroadcast_DoWork;
            workerBroadcast.RunWorkerCompleted += workerBroadcast_RunWorkerCompleted;
            workerBroadcast.WorkerSupportsCancellation = true;
            workerBroadcast.RunWorkerAsync();
            StartListening();
            Trace.TraceInformation("Stato disconnesso.");
        }

        #region broadcast
        void workerBroadcast_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            IPEndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderRemote = (EndPoint)senderEP;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, PORT_BROADCAST);
            String stringMessageFind = "";
            int riprovaConnessione = 3;
            while (!worker.CancellationPending)
            {
                try
                {
                    using (socketBroadcast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        //serve per non bufferizzare i messaggi
                        socketBroadcast.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 0);
                        socketBroadcast.Bind(ipep);
                        byte[] messageFindBC = new byte[1024];

                        // adesso bisogna controllare se il messaggio ricevuto e' corretto o no
                        // CORRETTO --> Invio le info utili al CLIENT PER LA CONNESSIONE ed esco dal while
                        // NON CORRETTO --> Mi rimetto in ascolto per un nuovo messaggio
                        while (stringMessageFind != TipoComando.FIND_SERVER)
                        {
                            if (workerBroadcast.CancellationPending) { return; }
                            socketBroadcast.ReceiveFrom(messageFindBC, ref senderRemote);
                            //m'interessano solo i primi 6 byte per vedere se è un messaggio di FIND BROADCAST
                            Trace.TraceInformation("Rischiesta di indirizzo ricevuta.");
                            stringMessageFind = Encoding.ASCII.GetString(messageFindBC, 0, 6);
                            // effettuo una Trim del messaggio per eliminare gli spazi vuoti in modo da controllare la correttezza del tipo di messaggio "FIND_S"
                            stringMessageFind.Trim();
                            stringMessageFind = "";
                            break;
                        }
                    }
                    // INVIO LE CREDENZIALI
                    IPAddress senderAddress = ((IPEndPoint)senderRemote).Address;
                    using (UdpClient client = new UdpClient())
                    {
                        IPEndPoint ipepClient = new IPEndPoint(senderAddress, PORT_BROADCAST_CLIENT);
                        int init = 0;
                        byte[] messageToSend = new byte[1024];
                        // devo inviare un messaggio del tipo "SERVER/NOMESERVER/INDIRIZZO/PORTA" senza spazi
                        Array.Copy(Encoding.ASCII.GetBytes(TipoComando.SERVER_INFORMATION), 0, messageToSend, init, 6);
                        init += 6;
                        byte[] nameServerArray = Encoding.ASCII.GetBytes(connectionParameter.ServerName);
                        Array.Copy(nameServerArray, 0, messageToSend, init, connectionParameter.ServerName.Length);
                        init += nameServerArray.Length;
                        // dato la lunghezza del nome Server è max di 20 BYTE, nel caso in cui è minore lo riempio con spazi vuoti
                        byte stuff = Convert.ToByte(' ');
                        for (; init < 26; init++)
                        {
                            messageToSend[init] = stuff;
                        }
                        // prendo 4 byte perchè utilizzo solo indirizzi IPv4
                        Array.Copy(IPAddress.Parse(connectionParameter.Address).GetAddressBytes(), 0, messageToSend, init, 4);
                        init += 4;
                        Array.Copy(BitConverter.GetBytes(connectionParameter.TcpPort), 0, messageToSend, init, 2);
                        init += 2;
                        // cosi invio il messaggio al CLIENT
                        client.Send(messageToSend, init, ipepClient);
                        Trace.TraceInformation("Indirizzo inviato.");
                    }
                }
                catch (SocketException)
                {
                    Trace.TraceInformation("Broadcast disattivato");
                    riprovaConnessione--;
                    if (riprovaConnessione <= 0)
                    {
                        e.Result = CLIENT_ERROR_RESULT;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Exception in workerBroadcast_DoWork(). Stack trace:\n{0}\n", ex.StackTrace);
                    return;
                }
            }
        }

        void workerBroadcast_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && e.Result != null)
            {
                if (e.Result.Equals(CLIENT_ERROR_RESULT))
                {
                    ClientErrorEventArgs args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.CONNECTION_ERROR };
                    connection.ClientErrorMessageReceived(args);
                }
            }
        }

        #endregion

        #region Control

        private void StartListening()
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(connectionParameter.Address), connectionParameter.TcpPort);
                listener = new TcpListener(endPoint);
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), this);
            }
            catch (SocketException se)
            {
                Trace.TraceError("SocketException in StartListening(). Stack trace:\n{0}\n", se.StackTrace);
                ClientErrorEventArgs args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.CONNECTION_ERROR };
                socketBroadcast.Close();
                workerBroadcast.CancelAsync();
                connection.ClientErrorMessageReceived(args);
                connection.CloseConnectionMessageRecived();
                              
            }
        }

        public static void DoAcceptTcpClientCallback(IAsyncResult ar)
        {
            try
            {
                // Get the listener that handles the client request.
                AbstractDisconnectedState disconnectedState = (AbstractDisconnectedState)ar.AsyncState;
                TcpListener listener = disconnectedState.listener;
                // End the operation
                disconnectedState.tcpControlConnection = listener.EndAcceptTcpClient(ar);
                //Start the Background worker for the login 
                disconnectedState.controlConnectionListener.RunWorkerAsync();
            }
            catch (ObjectDisposedException ode)
            {
                //quando si preme Stop e lui ancora è in attesa di connessione
                Trace.TraceWarning("ObjectDisposedException in DoAcceptTcpClientCallback(). Stack trace:\n{0}\n", ode.StackTrace);
            }
        }

        private void controlConnectionListener_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            NetworkStream stream = null;
            try
            {
                //Una volta aperta la connessione aspetto un messaggio per la connessione
                stream = tcpControlConnection.GetStream();
                stream.ReadTimeout = Connection.TIMEOUT;

                byte[] messageByte = new byte[1024];
                ReadMessage(stream, messageByte, 0, COMMAND_LENGHT);//la lunghezza dei messaggi è fissa

                string message = Encoding.ASCII.GetString(messageByte, 0, 6);

                //se il messaggio non è di login mi rimetto in ascolto 
                if (message != TipoComando.REQUEST_LOGIN)
                {
                    e.Result = LOGIN_ERROR_RESULT;
                    return;
                }
                if (connectionParameter.PasswordEnabled)
                {
                    //se il messaggio è di login invio il challange 
                    //genero un valore casuale
                    byte[] randomBytes = new byte[CHALLANGE_LENGHT];
                    Random random = new Random();
                    random.NextBytes(randomBytes);

                    //Controllo se ho ricevuto una richiesta di interruzione
                    if (worker.CancellationPending)
                    {
                        WriteMessage(stream, TipoComandoBytes.CLOSE_CONNECTION, 0, COMMAND_LENGHT);
                        e.Result = STOPPED_RESULT;
                        return;
                    }
                    //invio il challange
                    WriteMessage(stream, Encoding.ASCII.GetBytes(TipoComando.LOGIN_CHALLENGE), 0, COMMAND_LENGHT);
                    WriteMessage(stream, randomBytes, 0, CHALLANGE_LENGHT);

                    //Calcolo hash della password+challange
                    byte[] passowordBytes = new byte[connectionParameter.Password.Length + CHALLANGE_LENGHT];
                    Array.Copy(Encoding.ASCII.GetBytes(connectionParameter.Password), passowordBytes, connectionParameter.Password.Length);
                    Array.Copy(randomBytes, 0, passowordBytes, connectionParameter.Password.Length, randomBytes.Length);
                    SHA256 mySha = SHA256Managed.Create();
                    byte[] hash = mySha.ComputeHash(passowordBytes);

                    //mi metto in attesa di richiesta di login con challange
                    ReadMessage(stream, messageByte, 0, COMMAND_LENGHT);
                    message = Encoding.ASCII.GetString(messageByte, 0, 6);

                    //se il messaggio non è di login mi rimetto in ascolto 
                    if (message != TipoComando.REQUEST_LOGIN)
                    {
                        e.Result = LOGIN_ERROR_RESULT;
                        return;
                    }

                    byte[] hashReceived = new byte[256 / 8];
                    ReadMessage(stream, hashReceived, 0, hashReceived.Length);

                    //controllo se il challange è verificato
                    if (!Equals(hash, hashReceived))
                    {
                        //se non sono uguali invio un messaggio di errore e chiudo la comunicazione
                        WriteMessage(stream, Encoding.ASCII.GetBytes(TipoComando.LOGIN_ERROR), 0, COMMAND_LENGHT);
                        e.Result = LOGIN_ERROR_RESULT;
                        return;
                    }
                }
                //Controllo se ho ricevuto una richiesta di interruzione
                if (worker.CancellationPending)
                {
                    WriteMessage(stream, TipoComandoBytes.CLOSE_CONNECTION, 0, COMMAND_LENGHT);
                    e.Result = STOPPED_RESULT;
                    return;
                }
                //in questo caso la password è confermata
                //invio messaggio di conferma e i parametri di connessione
                WriteMessage(stream, Encoding.ASCII.GetBytes(TipoComando.LOGIN_OK), 0, COMMAND_LENGHT);

                //Controllo se ho ricevuto una richiesta di interruzione
                if (worker.CancellationPending)
                {
                    WriteMessage(stream, TipoComandoBytes.CLOSE_CONNECTION, 0, COMMAND_LENGHT);
                    e.Result = STOPPED_RESULT;
                    return;
                }
                WriteMessage(stream, Encoding.ASCII.GetBytes(TipoComando.LOGIN_UDP_PORT), 0, COMMAND_LENGHT);
                //Porta UDP se abilitato
                short udpPort = 0;
                if (connectionParameter.UdpEnabled)
                {
                    udpPort = connectionParameter.UdpPort;
                }
                WriteMessage(stream, BitConverter.GetBytes(udpPort), 0, 2);

                //Ricevo le dimensioni dello schermo da parte del client 
                byte[] comandoMonitor = new byte[1024];
                ReadMessage(stream, comandoMonitor, 0, COMMAND_LENGHT);
                String widthMessage = Encoding.ASCII.GetString(comandoMonitor, 0, 6);

                byte[] screenByte = new byte[1024];
                ReadMessage(stream, screenByte, 0, 4);
                width = BitConverter.ToUInt32(screenByte, 0);
                ReadMessage(stream, comandoMonitor, 0, COMMAND_LENGHT);
                String heightMessage = Encoding.ASCII.GetString(comandoMonitor, 0, 6);
                ReadMessage(stream, screenByte, 0, 4);
                height = BitConverter.ToUInt32(screenByte, 0);

                //Controllo se ho ricevuto una richiesta di interruzione
                if (worker.CancellationPending)
                {
                    e.Result = STOPPED_RESULT;
                }
                else
                {   //Connessione completata
                    e.Result = CONNECTED_RESULT;
                }
                return;

            }
            catch (NetworkException)
            {
                e.Result = NETWORK_ERROR_RESULT;
            }
            catch (Exception se)
            {
                if (se is SocketException || se is IOException)
                {
                    //Se è scaduto il timeout mi rimetto in ascolto
                    SocketException sockExc = se.InnerException as SocketException;
                    if (sockExc != null && sockExc.SocketErrorCode == SocketError.TimedOut)
                    {
                        e.Result = LOGIN_ERROR_RESULT;
                        return;
                    }
                    e.Result = CLIENT_ERROR_RESULT;
                }
                else
                {
                    e.Result = GENERIC_ERROR_RESULT;
                }
                Trace.TraceError("Exception in controlConnectionListener_DoWork(). Stack trace:\n{0}\n", se.StackTrace);
            }
        }

        private void controlConnectionListener_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && e.Result != null)
            {
                ClientErrorEventArgs args = null;
                string result = (string)e.Result;
                if (result.Equals(CONNECTED_RESULT))
                {
                    //Passo allo stato connesso
                    IConnectionState state = NextState(State.CONNECTED);
                    connection.State = state;
                    //chiudo il socket del broadcast xk deve essere
                    socketBroadcast.Close();
                    workerBroadcast.CancelAsync();
                    connection.ConnectClientMessageRecived(((IPEndPoint)tcpControlConnection.Client.RemoteEndPoint).Address.ToString(), width, height);
                    Trace.TraceInformation("Connesso.");
                }
                else
                {
                    //Altri casi
                    //Chiudo la connessione
                    tcpControlConnection.GetStream().Close();
                    tcpControlConnection.Close();
                    tcpControlConnection = null;
                    switch (result)
                    {
                        case STOPPED_RESULT:
                            connection.CloseConnectionMessageRecived();
                            break;

                        case NETWORK_ERROR_RESULT:
                            args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.NETWORK_ERROR };
                            connection.ClientErrorMessageReceived(args);
                            connection.CloseConnectionMessageRecived();
                            break;

                        case CLIENT_ERROR_RESULT:
                            args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.CLIENT_ERROR };
                            connection.ClientErrorMessageReceived(args);
                            connection.CloseConnectionMessageRecived();
                            break;

                        case GENERIC_ERROR_RESULT:
                            args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.GENERIC_ERROR };
                            connection.ClientErrorMessageReceived(args);
                            connection.CloseConnectionMessageRecived();
                            break;
                        case LOGIN_ERROR_RESULT:
                            listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), this);
                            return;
                    }
                }
                listener.Stop();

            }

        }

        #endregion

        public void Stop()
        {
            if (controlConnectionListener.IsBusy)
            {
                controlConnectionListener.CancelAsync();
            }
            else
            {
                listener.Stop();
                socketBroadcast.Close();
                workerBroadcast.CancelAsync();
            }
            Trace.TraceInformation("Stop.");
        }

        #region metodiPrivati

        private bool Equals(byte[] hash, byte[] hashReceived)
        {
            if (hash.Length != hashReceived.Length) { return false; }
            for (int i = 0; i < hash.Length; i++)
            {
                if (hash[i] != hashReceived[i]) { return false; }
            }
            return true;
        }

        private void ReadMessage(NetworkStream stream, byte[] messageByte, int offset, int size)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                while (offset < size)
                {
                    int byteLetti = stream.Read(messageByte, offset, size - offset);
                    if (byteLetti == 0)
                    {
                        throw new IOException("No data avaible to read", new SocketClosedException());
                    }
                    offset += byteLetti;
                }
            }
            else { throw new NetworkException(); }
        }

        private void WriteMessage(NetworkStream stream, byte[] msg, int start, int dim)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                stream.Write(msg, start, dim);
            }
            else { throw new NetworkException(); }
        }
        #endregion

        abstract protected IConnectionState NextState(State state);
    }

    public class UdpDisconnectedState : AbstractDisconnectedState
    {
        public UdpDisconnectedState(Connection connection, ConnectionParameter param)
            : base(connection, param) { }

        protected override IConnectionState NextState(State state)
        {
            switch (state)
            {
                case State.CONNECTED:
                    return new UdpConnectedState(connection, connectionParameter, tcpControlConnection);
                default:                    
                    throw new ArgumentException("Next State non valido: " + state.ToString());
            }
        }
    }

    public class TcpDisconnectedState : AbstractDisconnectedState
    {
        public TcpDisconnectedState(Connection connection, ConnectionParameter param)
            : base(connection, param) { }

        protected override IConnectionState NextState(State state)
        {
            switch (state)
            {
                case State.CONNECTED:
                    return new TcpConnectedState(connection, connectionParameter, tcpControlConnection);
                default:
                    throw new ArgumentException("Next State non valido: " + state.ToString());
            }
        }
    }

}
