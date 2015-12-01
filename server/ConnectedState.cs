using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerWPF
{
    public abstract class AbstractConnectedState : IConnectionState
    {
        private const string ACTIVE_RESULT = "ACTIVE_RESULT";
        private const string CLOSE_RESULT = "CLOSE_RESULT";
        private const string NETWORK_ERROR_RESULT = "NETWORK_ERROR_RESULT";
        private const string CLIENT_ERROR_RESULT = "CLIENT_ERROR_RESULT";
        private const string GENERIC_ERROR_RESULT = "GENERIC_ERROR_RESULT";
        protected const string EXTERNAL_CLOSE_RESULT = "EXTERNAL_CLOSE_RESULT";
        private const int COMMAND_LENGHT = 6;

        protected InnerState innerState;
        protected ConnectionParameter param;
        protected Connection connection;
        protected TcpClient tcpControlConnection;
        protected NetworkStream stream;

        private BackgroundWorker controlConnectionListener;
        private bool isStopped;

        public AbstractConnectedState(Connection connection, ConnectionParameter param, TcpClient client)
        {
            isStopped = false;
            this.tcpControlConnection = client;
            this.stream = tcpControlConnection.GetStream();
            this.param = param;
            this.connection = connection;
            controlConnectionListener = new BackgroundWorker();
            controlConnectionListener.DoWork += controlConnectionListener_DoWork;
            controlConnectionListener.RunWorkerCompleted += controlConnectionListener_RunWorkerCompleted;
            controlConnectionListener.WorkerSupportsCancellation = true;
            controlConnectionListener.WorkerReportsProgress = true;
            controlConnectionListener.ProgressChanged += controlConnectionListener_ProgressChanged;
            controlConnectionListener.RunWorkerAsync();
            Trace.TraceInformation("Stato connesso.");
        }

        void controlConnectionListener_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            bool keepAliveSended = false;
            if (tcpControlConnection == null)
            {
                e.Result = CLOSE_RESULT;
                return;
            }
            NetworkStream stream = tcpControlConnection.GetStream();
            while (!worker.CancellationPending)
            {
                try
                {
                    if (keepAliveSended)
                    {
                        worker.ReportProgress(0, TipoComandoBytes.KEEP_ALIVE_REQUEST);
                        Trace.TraceInformation("Richiesta di Keepalive inviata al client.");
                    }
                    byte[] messageByte = new byte[1024];
                    ReadMessage(stream, messageByte, 0, COMMAND_LENGHT);//la lunghezza dei messaggi è fissa
                    string message = Encoding.ASCII.GetString(messageByte, 0, COMMAND_LENGHT);
                    switch (message)
                    {
                        case TipoComando.ACTIVE_SERVER:
                            e.Result = ACTIVE_RESULT;
                            Trace.TraceInformation("Richiesta di attivazione da parte del client.");
                            return;
                        case TipoComando.CLOSE_CONNECTION:
                            e.Result = CLOSE_RESULT;
                            Trace.TraceInformation("Richiesta di chiusura da parte del client.");
                            return;
                        case TipoComando.KEEP_ALIVE_REQUEST:
                            keepAliveSended = false;
                            Trace.TraceInformation("Richiesta di Keepalive ricevuta.");
                            worker.ReportProgress(0, TipoComandoBytes.KEEP_ALIVE_ACK);
                            Trace.TraceInformation("Keepalive ack inviato.");
                            break;
                        case TipoComando.KEEP_ALIVE_ACK:
                            //ho ricevuto il messaggio di KEEP_ALIVE_ACK, 
                            //quindi reimposto il boolean keepAliveSended a false
                            keepAliveSended = false;
                            Trace.TraceInformation("Keepalive ack ricevuto.");
                            break;
                        default:
                            //lancio l'eccezione nel caso di messaggio imprevisto
                            throw new Exception("Invalid messagge received: \'" + message + "\' ");
                    }
                }
                catch (NetworkException)
                {
                    Trace.TraceError("NetworkException in controlConnectionListener_DoWork().");
                    e.Result = NETWORK_ERROR_RESULT;
                    return;
                }
                catch (SocketException se)
                {
                    Trace.TraceError("SocketException in controlConnectionListener_DoWork(). Stack trace:\n{0}\nSocket error:{1}\n", se.StackTrace, se.SocketErrorCode);
                    e.Result = CLIENT_ERROR_RESULT;
                    return;
                }
                catch (IOException ioe)
                {
                    //Se è scaduto il timeout mi rimetto in ascolto
                    SocketException sockExc = ioe.InnerException as SocketException;
                    if (sockExc != null && sockExc.SocketErrorCode == SocketError.TimedOut)
                    {
                        //mi preparo ad inviare la richiesta di keepAlive se non l'ho inviato in precedenza
                        if (!keepAliveSended)
                        {
                            Trace.TraceInformation("Timeout scaduto.");
                            keepAliveSended = true;
                        }
                        else
                        {
                            Trace.TraceError("Keepalive ack non ricevuto.");
                            e.Result = CLIENT_ERROR_RESULT;
                            return;
                        }
                    }
                    else
                    {
                        Trace.TraceError("IOException in controlConnectionListener_DoWork(). Stack trace:\n{0}\n", ioe.StackTrace);
                        e.Result = CLIENT_ERROR_RESULT;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception in controlConnectionListener_DoWork(). Stack trace:\n{0}\n", ex.StackTrace);
                    e.Result = CLIENT_ERROR_RESULT;
                    return;
                }
            }
            e.Result = EXTERNAL_CLOSE_RESULT;
        }
        void controlConnectionListener_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                WriteMessage(stream, (byte[])e.UserState, 0, COMMAND_LENGHT);
            }
            catch (NetworkException)
            {
                Trace.TraceError("NetworkException in controlConnectionListener_ProgressChanged().");
                innerState = InnerState.STOP_BY_NETWORK_ERROR;
                controlConnectionListener.CancelAsync();
            }
            catch (SocketException se)
            {
                Trace.TraceError("SocketException in controlConnectionListener_ProgressChanged(). Stack trace:\n{0}\nSocket error:{1}\n", se.StackTrace, se.SocketErrorCode);
                innerState = InnerState.STOP_BY_ERROR;
                controlConnectionListener.CancelAsync();
            }
            catch (IOException ioe)
            {
                Trace.TraceError("IOException in controlConnectionListener_ProgressChanged(). Stack trace:\n{0}\n", ioe.StackTrace);
                innerState = InnerState.STOP_BY_ERROR;
                controlConnectionListener.CancelAsync();
            }
        }
        void controlConnectionListener_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Interrompo gli altri thread
            ClientErrorEventArgs args = null;
            switch (innerState)
            {
                case InnerState.STOP_BY_NETWORK_ERROR:
                    connection.State = null;
                    args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.NETWORK_ERROR };
                    connection.ClientErrorMessageReceived(args);
                    if (tcpControlConnection != null)
                    {
                        stream.Close();
                        tcpControlConnection.Close();
                    }
                    connection.CloseConnectionMessageRecived();
                    return;

                case InnerState.STOP_BY_ERROR:
                    connection.State = null;
                    args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.CLIENT_ERROR };
                    connection.ClientErrorMessageReceived(args);
                    if (tcpControlConnection != null)
                    {
                        stream.Close();
                        tcpControlConnection.Close();
                    }
                    connection.CloseConnectionMessageRecived();
                    return;

                case InnerState.STOP_BY_USER:
                    connection.State = null;
                    if (tcpControlConnection != null)
                    {
                        stream.Close();
                        tcpControlConnection.Close();
                    }
                    connection.CloseConnectionMessageRecived();
                    return;
            }

            if (e.Error == null && e.Result != null)
            {
                string result = (string)e.Result;
                switch (result)
                {
                    case ACTIVE_RESULT:
                        //cambio lo stato in connesso 
                        connection.State = NextState(State.ACTIVE);
                        connection.ActiveClientMessageRecived();
                        break;
                    case CLOSE_RESULT:
                        //passo allo stato disconnected
                        innerState = InnerState.STOP_BY_CONTROLLER;
                        connection.State = NextState(State.DISCONNECTED);
                        connection.DisconnectClientMessageRecived();
                        if (tcpControlConnection != null)
                        {
                            stream.Close();
                            tcpControlConnection.Close();
                        }
                        break;

                    case EXTERNAL_CLOSE_RESULT://Questo caso non dovrebbe accadere
                        
                        try
                        {
                            if (stream != null) { WriteMessage(stream, TipoComandoBytes.CLOSE_CONNECTION, 0, COMMAND_LENGHT); }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Exception in controlConnectionListener_RunWorkerComplited(). Stack trace:\n{0}\n", ex.StackTrace);
                            if (!(ex is NetworkException || ex is IOException || ex is SocketException)) { throw; }
                        }
                        finally
                        {
                            if (tcpControlConnection != null)
                            {
                                stream.Close();
                                tcpControlConnection.Close();
                            }
                            connection.CloseConnectionMessageRecived();
                        }
                        break;
                    case NETWORK_ERROR_RESULT:
                        innerState = InnerState.STOP_BY_NETWORK_ERROR;
                        connection.State = null;
                        args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.NETWORK_ERROR };
                        connection.ClientErrorMessageReceived(args);
                        if (tcpControlConnection != null)
                        {
                            stream.Close();
                            tcpControlConnection.Close();
                        }
                        connection.CloseConnectionMessageRecived();
                        break;

                    case CLIENT_ERROR_RESULT:
                        innerState = InnerState.STOP_BY_ERROR;
                        connection.State = null;
                        args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.CLIENT_ERROR };
                        connection.ClientErrorMessageReceived(args);
                        if (tcpControlConnection != null)
                        {
                            stream.Close();
                            tcpControlConnection.Close();
                        }
                        connection.CloseConnectionMessageRecived();
                        break;

                    case GENERIC_ERROR_RESULT:
                        innerState = InnerState.STOP_BY_ERROR;
                        connection.State = null;
                        args = new ClientErrorEventArgs() { ErrorCode = ClientErrorEventArgs.GENERIC_ERROR };
                        connection.ClientErrorMessageReceived(args);
                        if (tcpControlConnection != null)
                        {
                            stream.Close();
                            tcpControlConnection.Close();
                        }
                        connection.CloseConnectionMessageRecived();
                        break;
                }
            }
        }

        #region stop_Methods

        public void Stop()
        {
            try
            {
                innerState = InnerState.STOP_BY_USER;
                WriteMessage(stream, TipoComandoBytes.CLOSE_CONNECTION, 0, TipoComandoBytes.CLOSE_CONNECTION.Length);
                controlConnectionListener.CancelAsync();
            }
            catch (Exception e)
            {      /*non devo fare nulla perché sto chiudendo*/
                Trace.TraceError("Exception in Stop(). Stack trace:\n{0}\n", e.StackTrace);
            }
        }

        #endregion

        protected abstract IConnectionState NextState(State state);

        #region metodiPrivati

        private void ReadMessage(NetworkStream stream, byte[] messageByte, int offset, int size)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                if (stream != null)
                {
                    while (offset < size)
                    {
                        int byteLetti = stream.Read(messageByte, offset, size - offset);
                        if (byteLetti == 0)
                        {
                            throw new IOException("No data avaible to read");
                        }
                        offset += byteLetti;
                    }
                }
            }
            else
            {
                throw new NetworkException();
            }
        }
        private void WriteMessage(NetworkStream stream, byte[] msg, int start, int dim)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                if (stream != null)
                {
                    stream.Write(msg, start, dim);
                }
            }
            else
            {
                throw new NetworkException();
            }
        }

        #endregion
    }

    public class UdpConnectedState : AbstractConnectedState
    {
        public UdpConnectedState(Connection connection, ConnectionParameter param, TcpClient tcpControlConnection)
            : base(connection, param, tcpControlConnection) { }

        protected override IConnectionState NextState(State state)
        {
            switch (state)
            {
                case State.DISCONNECTED:
                    return new UdpDisconnectedState(connection, param);
                case State.ACTIVE:
                    return new UdpActivatedState(connection, param, tcpControlConnection);
                default:
                    throw new ArgumentException("Next State non valido: " + state.ToString());
            }
        }
    }

    public class TcpConnectedState : AbstractConnectedState
    {
        public TcpConnectedState(Connection connection, ConnectionParameter param, System.Net.Sockets.TcpClient tcpControlConnection)
            : base(connection, param, tcpControlConnection) { }
        protected override IConnectionState NextState(State state)
        {
            switch (state)
            {
                case State.DISCONNECTED:
                    return new TcpDisconnectedState(connection, param);
                case State.ACTIVE:
                    return new TcpActivatedState(connection, param, tcpControlConnection);
                default:
                    throw new ArgumentException("Next State non valido: " + state.ToString());
            }
        }
    }
}
