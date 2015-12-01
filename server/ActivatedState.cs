using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ServerWPF
{

    public abstract class AbstractActivatedState : IConnectionState
    {

        private const int TIMEOUT = 5000;
        protected const int COMMAND_LENGHT = 6;
        protected const string CLOSE_RESULT = "CLOSE_RESULT";
        protected const string EXTERNAL_CLOSE_RESULT = "EXTERNAL_CLOSE_RESULT";
        protected const string DEACTIVE_RESULT = "DEACTIVE_RESULT";
        protected const string NETWORK_ERROR_RESULT = "NETWORK_ERROR_RESULT";
        protected const string CLIENT_ERROR_RESULT = "CLIENT_ERROR_RESULT";
        protected const string GENERIC_ERROR_RESULT = "GENERIC_ERROR_RESULT";
        protected const string REMOTE_COPY_EXECUTED = "REMOTE_COPY_EXECUTED";
        protected const string REMOTE_PASTE_EXECUTED = "REMOTE_PASTE_EXECUTED";
        protected const string TRANSFER_CANCELLED = "TRANSFER_CANCELLED";

        protected InnerState innerState;
        protected Connection connection;
        protected ConnectionParameter param;
        protected MouseExecutor mouse;
        protected TcpClient tcpControlConnection;
        protected Socket mouseSocket;

        private BackgroundWorker controlConnectionListener;
        private BackgroundWorker clipboardWorker;
        private BackgroundWorker mouseWorker;
        private TcpClient tcpClipboardConnection;
        private NetworkStream stream;
        private MyClibpoard clipboard;
        private KeyboardExecutor keyboard;

        public AbstractActivatedState(Connection connection, ConnectionParameter param, TcpClient tcpControlConnection)
        {
            innerState = InnerState.ACTIVE;
            this.connection = connection;
            this.param = param;
            this.tcpControlConnection = tcpControlConnection;
            this.stream = tcpControlConnection.GetStream();
            this.mouse = new MouseExecutor(connection.HeightClientMonitor, connection.WidthClientMonitor);
            this.keyboard = new KeyboardExecutor();
            this.clipboard = null;

            controlConnectionListener = new BackgroundWorker();
            controlConnectionListener.WorkerSupportsCancellation = true;
            controlConnectionListener.WorkerReportsProgress = true;
            controlConnectionListener.DoWork += controlConnectionListener_DoWork;
            controlConnectionListener.RunWorkerCompleted += controlConnectionListener_RunWorkerCompleted;
            controlConnectionListener.ProgressChanged += controlConnectionListener_ProgressChanged;
            controlConnectionListener.RunWorkerAsync();

            clipboardWorker = new BackgroundWorker();
            clipboardWorker.WorkerSupportsCancellation = true;
            clipboardWorker.WorkerReportsProgress = true;
            clipboardWorker.DoWork += clipboardWorker_DoWork;
            clipboardWorker.RunWorkerCompleted += clipboardWorker_RunWorkerCompleted;
            clipboardWorker.ProgressChanged += clipboardWorker_ProgressChanged;

            ListeningForMouse();
        }

        protected abstract IConnectionState NextState(State state);
        protected abstract Socket CreateSocket();
        protected abstract void ReadMessageMouseSocket();

        #region Control
        void controlConnectionListener_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            bool keepAliveSended = false;
            if (tcpControlConnection == null)
            {
                e.Result = CLOSE_RESULT;
                return;
            }

            while (!worker.CancellationPending)
            {
                try
                {
                    if (keepAliveSended)
                    {   //Invio KEEP ALIVE REQUEST: Tmeout scaduto e keepAliveSended == true;                                       
                        worker.ReportProgress(0, TipoComandoBytes.KEEP_ALIVE_REQUEST);
                        Trace.TraceInformation("Richiesta di Keepalive inviata al client.");
                    }
                    byte[] messageByte = new byte[1024];
                    ReadMessage(stream, messageByte, 0, COMMAND_LENGHT);//la lunghezza dei messaggi è fissa
                    string message = Encoding.ASCII.GetString(messageByte, 0, COMMAND_LENGHT);
                    byte[] code = new byte[1];
                    switch (message)
                    {
                        case TipoComando.KEY_PRESS_DOWN:
                            ReadMessage(stream, code, 0, 1);
                            keyboard.KeyPressDown(code[0]);
                            break;
                        case TipoComando.KEY_PRESS_UP:
                            ReadMessage(stream, code, 0, 1);
                            keyboard.KeyPressUp(code[0]);
                            break;
                        case TipoComando.DEACTIVE_SERVER:
                            e.Result = DEACTIVE_RESULT;
                            return;
                        case TipoComando.CLOSE_CONNECTION:
                            e.Result = CLOSE_RESULT;
                            return;
                        case TipoComando.KEEP_ALIVE_REQUEST:
                            //invio il messaggio di KEEP ALIVE ACK
                            Trace.TraceInformation("Richiesta di Keepalive ricevuta.");
                            worker.ReportProgress(0, TipoComandoBytes.KEEP_ALIVE_ACK);
                            Trace.TraceInformation("Keepalive ack inviato.");
                            break;
                        case TipoComando.KEEP_ALIVE_ACK:
                            keepAliveSended = false;
                            Trace.TraceInformation("Keepalive ack ricevuto.");
                            break;
                        case TipoComando.ACTIVE_CLIPBOARD_RC:
                            //Ho ricevuto una richiesta per attivare il RemoteCopy
                            worker.ReportProgress(100, true);
                            break;
                        case TipoComando.ACTIVE_CLIPBOARD_RP:
                            //Ho ricevuto una richiesta per attivare il RemotePaste
                            worker.ReportProgress(100, false);
                            break;
                        default:
                            //lancio l'eccezione nel caso di messaggio imprevisto
                            throw new Exception("Invalid messagge received: \'" + message + "\' ");
                    }
                    keepAliveSended = false;
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
                switch (e.ProgressPercentage)
                {
                    case 0:
                        WriteMessage(stream, (byte[])e.UserState, 0, COMMAND_LENGHT);
                        break;
                    case 100:
                        bool isRemoteCopy = (bool)e.UserState;
                        ListeningForClipboard(isRemoteCopy);
                        break;
                }
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
            StopMouseListening();
            StopClipboardListening();
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
                    case DEACTIVE_RESULT:
                        //cambio lo stato in connesso 
                        innerState = InnerState.STOP_BY_CONTROLLER;
                        connection.State = NextState(State.CONNECTED);
                        connection.ConnectClientMessageRecived(null, 0, 0);
                        forceButtonUp();
                        break;
                    case CLOSE_RESULT:
                        //passo allo stato disconnected
                        innerState = InnerState.STOP_BY_CONTROLLER;
                        connection.State = NextState(State.DISCONNECTED);
                        connection.DisconnectClientMessageRecived();
                        forceButtonUp();
                        if (tcpControlConnection != null)
                        {
                            stream.Close();
                            tcpControlConnection.Close();
                        }
                        break;

                    case EXTERNAL_CLOSE_RESULT:
                        
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
        #endregion

        #region Clipboard
        void clipboardWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            bool remoteCopy = (bool)e.Argument;
            TcpListener listener = null;
            try
            {
                //apro connessione verso il client
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(param.Address), param.TcpPort + 1);
                listener = new TcpListener(endPoint);
                listener.Start();

                //Invio messaggio di clipboard ack nel main thread
                worker.ReportProgress(0);

                using (tcpClipboardConnection = listener.AcceptTcpClient())
                {
                    tcpClipboardConnection.GetStream().ReadTimeout = TIMEOUT;

                    if (remoteCopy)
                    {
                        DoRemoteCopy();
                        if (!worker.CancellationPending)
                        {
                            e.Result = REMOTE_COPY_EXECUTED;
                        }
                    }
                    else
                    {
                        DoRemotePaste();
                        if (!worker.CancellationPending)
                        {
                            e.Result = REMOTE_PASTE_EXECUTED;
                        }
                    }
                }
            }
            catch (NetworkException)
            {
                Trace.TraceError("NetworkException in clipboardWorker_DoWork().");
                e.Result = NETWORK_ERROR_RESULT;
                return;
            }
            catch (SocketException se)
            {
                Trace.TraceError("SocketException in clipboardWorker_DoWork(). Stack trace:\n{0}\nSocket error:{1}\n", se.StackTrace, se.SocketErrorCode);
                e.Result = CLIENT_ERROR_RESULT;
                return;
            }
            catch (IOException ioe)
            {

                if (ioe.InnerException != null)
                {
                    if (ioe.InnerException is SocketException && ((SocketException)ioe.InnerException).SocketErrorCode != SocketError.TimedOut)
                    {
                        // questo può avvenire quando il client annulla il trasferimento del FILE
                        Trace.TraceInformation("Trasferimento cancellato.");
                        e.Result = TRANSFER_CANCELLED;
                    }
                    else if (ioe.InnerException is SocketClosedException)
                    {
                        Trace.TraceInformation("Trasferimento cancellato.");
                        e.Result = TRANSFER_CANCELLED;
                    }
                    else
                    {
                        Trace.TraceError("IOException in clipboardWorker_DoWork(). Stack trace:\n{0}\n", ioe.StackTrace);
                        e.Result = CLIENT_ERROR_RESULT;
                    }
                }
                else
                {
                    Trace.TraceError("IOException in clipboardWorker_DoWork(). Stack trace:\n{0}\n", ioe.StackTrace);
                    e.Result = CLIENT_ERROR_RESULT;
                }
                return;
            }
            finally
            {
                if (listener != null) { listener.Stop(); }
            }
        }

        void clipboardWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                //invio il messaggio di ack
                WriteMessage(stream, TipoComandoBytes.ACTIVE_CLIPBOARD_ACK, 0, TipoComandoBytes.ACTIVE_CLIPBOARD_ACK.Length);
            }
            catch (IOException ioe)
            {
                Trace.TraceError("IOException in clipboardWorker_ProgressChanged(). Stack trace:\n{0}\n", ioe.StackTrace);
                innerState = InnerState.STOP_BY_ERROR;
                if (controlConnectionListener.IsBusy) { controlConnectionListener.CancelAsync(); }
            }
            catch (SocketException se)
            {
                Trace.TraceError("SocketException in clipboardWorker_ProgressChanged(). Stack trace:\n{0}\nSocket error:{1}\n", se.StackTrace, se.SocketErrorCode);
                innerState = InnerState.STOP_BY_ERROR;
                if (controlConnectionListener.IsBusy) { controlConnectionListener.CancelAsync(); }
            }
            catch (NetworkException)
            {
                Trace.TraceError("NetworkException in clipboardWorker_ProgressChanged().");
                innerState = InnerState.STOP_BY_NETWORK_ERROR;
                if (controlConnectionListener.IsBusy) { controlConnectionListener.CancelAsync(); }
            }
        }

        void clipboardWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            tcpClipboardConnection = null;
            if (innerState == InnerState.STOP_BY_ERROR || innerState == InnerState.STOP_BY_NETWORK_ERROR) { return; }
            if (e.Result != null)
            {
                string result = e.Result as string;
                switch (result)
                {
                    case NETWORK_ERROR_RESULT:
                        innerState = InnerState.STOP_BY_NETWORK_ERROR;
                        if (controlConnectionListener.IsBusy) { controlConnectionListener.CancelAsync(); }
                        break;
                    case CLIENT_ERROR_RESULT:
                        innerState = InnerState.STOP_BY_ERROR;
                        if (controlConnectionListener.IsBusy) { controlConnectionListener.CancelAsync(); }
                        break;
                    case REMOTE_PASTE_EXECUTED:
                        //imposto la Clipboard di sistema
                        clipboard.CopyOnClipboard();
                        break;
                    case TRANSFER_CANCELLED:
                        //In questo caso non devo fare nulla perché il trasferimento è stato annullato
                        break;
                }
            }


        }

        private void ListeningForClipboard(bool remoteCopy)
        {
            if (!clipboardWorker.IsBusy)
            {
                clipboard = new MyClibpoard();
                if (remoteCopy)
                {
                    clipboard.AcquireClipboardContent();
                }
                clipboardWorker.RunWorkerAsync(remoteCopy);
            }
        }

        private void StopClipboardListening()
        {
            if (clipboardWorker.IsBusy) { clipboardWorker.CancelAsync(); }
        }

        private void DoRemotePaste()
        {
            using (BufferedStream stream = new BufferedStream(tcpClipboardConnection.GetStream()))
            {
                byte[] msg = new Byte[1024];
                //Ricevo il comando di lunghezza 6
                ReadMessage(stream, msg, 0, 6);
                String command = Encoding.ASCII.GetString(msg, 0, 6);
                //Ricevo il secondo parametro: il numero dei file 
                msg = new byte[4];
                ReadMessage(stream, msg, 0, 4);
                int numFile = BitConverter.ToInt32(msg, 0);
                //Ricevo la dimensione dei file
                ReadMessage(stream, msg, 0, 4);
                int dimensionFiles = BitConverter.ToInt32(msg, 0);
                //ora vedo il tipo di file ke si è richiesto di copiare
                if (command.Equals(TipoComando.CLIPBOARD_FILES))
                {
                    String pathFileToCopy = null;
                    StringCollection sc = new StringCollection();
                    String tipo;
                    String nome;
                    int lengthNumFile; //lunghezza nel caso di file, numero file in caso di directory
                    String tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
                    Directory.CreateDirectory(tempDirectory);
                    for (int i = 0; i < numFile && !clipboardWorker.CancellationPending; i++)
                    {
                        //Ricevo il tipo di file
                        msg = new byte[2];
                        ReadMessage(stream, msg, 0, msg.Length);
                        tipo = Encoding.ASCII.GetString(msg, 0, msg.Length);
                        //Ricevo il nome del file
                        msg = new byte[260];
                        ReadMessage(stream, msg, 0, msg.Length);
                        nome = Encoding.ASCII.GetString(msg, 0, msg.Length);
                        //Ricevo la lunghezza nel caso di file o numero file in caso di directory
                        msg = new byte[4];
                        ReadMessage(stream, msg, 0, msg.Length);
                        lengthNumFile = BitConverter.ToInt32(msg, 0);
                        nome = nome.Trim();
                        pathFileToCopy = System.IO.Path.Combine(tempDirectory, nome);
                        sc.Add(pathFileToCopy);
                        if (tipo.Equals(TipoComando.CLIPBOARD_FILE_TYPE_FILE))
                        {
                            CreateFile(stream, pathFileToCopy, lengthNumFile);
                        }
                        else if (tipo.Equals(TipoComando.CLIPBOARD_FILE_TYPE_DIRECTORY))
                        {
                            CreateDirectory(stream, pathFileToCopy, lengthNumFile);
                        }
                        else
                        {
                            Trace.TraceError("Protocollo violato in DoRemotePaste(). Tipo comando:{0}",tipo);
                            throw new Exception("Protocollo violato");
                        }
                    }

                    clipboard.FileDropList = sc;
                }
                else
                {
                    msg = new byte[dimensionFiles];
                    ReadMessage(stream, msg, 0, dimensionFiles);
                   
                    if (command.Equals(TipoComando.CLIPBOARD_IMAGE))
                    {
                        clipboard.Image = msg;
                    }
                    else if (command.Equals(TipoComando.CLIPBOARD_TEXT))
                    {
                        clipboard.Text = Encoding.Unicode.GetString(msg, 0, dimensionFiles);
                    }
                    else if (command.Equals(TipoComando.CLIPBOARD_AUDIO))
                    {
                        clipboard.Audio = new MemoryStream(msg);
                    }
                    else if (command.Equals(TipoComando.CLIPBOARD_EMPTY))
                    {
                        clipboard.isEmpty = true;                        
                    }
                    else
                    {
                        Trace.TraceError("Protocollo violato in DoRemotePaste(). Tipo comando:{0}", command);
                        throw new Exception("Protocollo violato");
                    }

                }

            }
        }
        private void DoRemoteCopy()
        {
            using (BufferedStream stream =new BufferedStream( tcpClipboardConnection.GetStream()))
            {
                byte[] command = null;
                byte[] numFiles = null;
                byte[] dimension = null;
                byte[] data = null;
                //Controllo se la clipboard contiene file
                if (clipboard.IsFile)
                {
                    StringCollection files = clipboard.FileDropList;
                    command = TipoComandoBytes.CLIPBOARD_FILES;
                    numFiles = BitConverter.GetBytes(files.Count);
                    int dim = CalcolaDimFiles(files);
                    dimension = BitConverter.GetBytes(dim);

                    WriteMessage(stream, command, 0, command.Length);
                    WriteMessage(stream, numFiles, 0, numFiles.Length);
                    WriteMessage(stream, dimension, 0, dimension.Length);

                    foreach (string file in files)
                    {
                        if (File.Exists(file) || Directory.Exists(file))
                        {
                            FileAttributes attr = File.GetAttributes(@file);
                            String[] token = file.Split('\\');
                            string nome = token[token.Length - 1];
                            if (attr.HasFlag(FileAttributes.Directory))
                            {
                                SendDir(stream, file, nome);
                            }
                            else
                            {
                                SendFile(stream, file, nome);
                            }
                        }
                    }
                }
                else if (clipboard.IsText)
                {
                    command = TipoComandoBytes.CLIPBOARD_TEXT;
                    numFiles = BitConverter.GetBytes(1);
                    data = Encoding.Unicode.GetBytes(clipboard.Text);
                    int dim = data.Length;
                    dimension = BitConverter.GetBytes(dim);

                    WriteMessage(stream, command, 0, command.Length);
                    WriteMessage(stream, numFiles, 0, numFiles.Length);
                    WriteMessage(stream, dimension, 0, dimension.Length);
                    WriteMessage(stream, data, 0, data.Length);
                }
                else if (clipboard.IsImage)
                {
                    command = TipoComandoBytes.CLIPBOARD_IMAGE;
                    numFiles = BitConverter.GetBytes(1);
                    data = clipboard.Image;
                    int dim = data.Length;
                    dimension = BitConverter.GetBytes(dim);

                    WriteMessage(stream, command, 0, command.Length);
                    WriteMessage(stream, numFiles, 0, numFiles.Length);
                    WriteMessage(stream, dimension, 0, dimension.Length);
                    WriteMessage(stream, data, 0, data.Length);
                }
                else if (clipboard.IsAudio)
                {
                    command = TipoComandoBytes.CLIPBOARD_AUDIO;
                    numFiles = BitConverter.GetBytes(1);
                    Stream audioStream = clipboard.Audio;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        audioStream.CopyTo(ms);
                        data = ms.ToArray();
                    }
                    int dim = data.Length;
                    dimension = BitConverter.GetBytes(dim);

                    WriteMessage(stream, command, 0, command.Length);
                    WriteMessage(stream, numFiles, 0, numFiles.Length);
                    WriteMessage(stream, dimension, 0, dimension.Length);
                    WriteMessage(stream, data, 0, data.Length);

                }
                else
                {
                    command = TipoComandoBytes.CLIPBOARD_EMPTY;
                    numFiles = BitConverter.GetBytes(0);
                    int dim = 0;
                    dimension = BitConverter.GetBytes(dim);

                    WriteMessage(stream, command, 0, command.Length);
                    WriteMessage(stream, numFiles, 0, numFiles.Length);
                    WriteMessage(stream, dimension, 0, dimension.Length);
                }

            }
        }
        private void CreateFile(Stream stream, String path, int length)
        {
           
            using (FileStream output = File.Create(path))
            {
                byte[] buffer = new byte[4096];
                int byteLettiTot = 0;
                int byteDaLeggere = 4096;
                while (byteLettiTot < length && !clipboardWorker.CancellationPending)
                {
                    if (length - byteLettiTot < 4096)
                    {
                        byteDaLeggere = length - byteLettiTot;
                    }
                    ReadMessage(stream, buffer, 0, byteDaLeggere);
                    output.Write(buffer, 0, byteDaLeggere);
                    byteLettiTot += byteDaLeggere;
                }
            }
            
        }
        private void CreateDirectory(Stream stream, String path, int length)
        {
            Directory.CreateDirectory(path);
            for (int i = 0; i < length && !clipboardWorker.CancellationPending; i++)
            {
                byte[] msg = new byte[2];
                ReadMessage(stream, msg, 0, msg.Length);
                String tipo = Encoding.ASCII.GetString(msg);
                msg = new byte[260];
                ReadMessage(stream, msg, 0, msg.Length);
                String nome = Encoding.ASCII.GetString(msg);
                msg = new byte[4];
                ReadMessage(stream, msg, 0, msg.Length);
                int j = BitConverter.ToInt32(msg, 0);
                String subPath = System.IO.Path.Combine(path, nome);
                if (tipo.Equals(TipoComando.CLIPBOARD_FILE_TYPE_FILE))
                {
                    CreateFile(stream, subPath, j);
                }
                else if (tipo.Equals(TipoComando.CLIPBOARD_FILE_TYPE_DIRECTORY))
                {
                    CreateDirectory(stream, subPath, j);
                }
                else
                {
                    Trace.TraceError("Protocollo violato in CreateDirectory(). Tipo comando:{0}", tipo);
                    throw new Exception("Protocollo Violato");
                }
            }
        }
        private void SendFile(Stream stream, string path, string nome)
        {
            if (clipboardWorker.CancellationPending) { return; }
            byte[] command = TipoComandoBytes.CLIPBOARD_FILE_TYPE_FILE;
            //array di byte di lunghezza 260 contenete il nome del file 
            byte[] nomeBytes = new byte[260];
            for (int i = 0; i < nomeBytes.Length; i++)
            {
                nomeBytes[i] = Convert.ToByte(' ');
            }

            Array.Copy(Encoding.ASCII.GetBytes(nome), nomeBytes, nome.Length);

            //array di byte contenente la lunghezza del file
            FileInfo fInfo = new FileInfo(path);

            byte[] fileLengthByte = BitConverter.GetBytes((int)fInfo.Length);

            //invio file
            WriteMessage(stream, command, 0, command.Length);
            WriteMessage(stream, nomeBytes, 0, nomeBytes.Length);
            WriteMessage(stream, fileLengthByte, 0, fileLengthByte.Length);
            int length = (int)fInfo.Length;
            using (FileStream inputFile = File.OpenRead(path))
            {                
                byte[] buffer = new byte[4096];
                int byteLettiTot = 0;
                int byteLetti = 0;
                int byteDaLeggere = 4096;
                while (byteLettiTot < length)
                {
                    if (length - byteLettiTot < 4096)
                    {
                        byteDaLeggere = length - byteLettiTot;
                    }

                    byteLetti = inputFile.Read(buffer, 0, byteDaLeggere);
                    if (clipboardWorker.CancellationPending) { return; }
                    WriteMessage(stream, buffer, 0, byteLetti);
                    byteLettiTot += byteLetti;
                }
                
            }
            Trace.TraceInformation("File inviato. File: {0}", path);
        }
        private void SendDir(Stream stream, string path, string nome)
        {
            String[] files = Directory.GetFiles(path);
            String[] directories = Directory.GetDirectories(path);
            int numFiles = files.Length + directories.Length;

            byte[] command = TipoComandoBytes.CLIPBOARD_FILE_TYPE_DIRECTORY;
            byte[] nomeBytes = new byte[260];

            for (int i = 0; i < nomeBytes.Length; i++)
            {
                nomeBytes[i] = Convert.ToByte(' ');
            }

            Array.Copy(Encoding.ASCII.GetBytes(nome), nomeBytes, nome.Length);
            byte[] numFilesByte = BitConverter.GetBytes(numFiles);

            WriteMessage(stream, command, 0, command.Length);
            WriteMessage(stream, nomeBytes, 0, nomeBytes.Length);
            WriteMessage(stream, numFilesByte, 0, numFilesByte.Length);

            foreach (string subFilePath in files)
            {
                string[] tokens = subFilePath.Split('\\');
                string n = tokens[tokens.Length - 1];
                SendFile(stream, subFilePath, n);
            }

            foreach (string subDirPath in directories)
            {
                string[] tokens = subDirPath.Split('\\');
                string n = tokens[tokens.Length - 1];
                SendDir(stream, subDirPath, n);
            }
        }
        private int CalcolaDimFiles(StringCollection files)
        {
            int dim = 0;
            foreach (string file in files)
            {
                FileAttributes attr = File.GetAttributes(@file);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    StringCollection subFiles = new StringCollection();
                    string[] f = Directory.GetFiles(file);
                    string[] d = Directory.GetDirectories(file);
                    subFiles.AddRange(f);
                    subFiles.AddRange(d);
                    dim += CalcolaDimFiles(subFiles);
                }
                else
                {
                    FileInfo fInfo = new FileInfo(file);
                    dim += (int)fInfo.Length;
                }
            }

            return dim;
        }

        #endregion

        #region Mouse
        private void MouseWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            try
            {
                mouseSocket = CreateSocket();
                while (!worker.CancellationPending)
                {
                    ReadMessageMouseSocket();
                }
            }
            catch (NetworkException)
            {
                Trace.TraceError("NetworkException in MouseWorker_DoWork().");
                e.Result = NETWORK_ERROR_RESULT;
                return;
            }
            catch (SocketException se)
            {
                Trace.TraceError("SocketException in MouseWorker_DoWork(). Stack trace:\n{0}\nSocket error:{1}\n", se.StackTrace, se.SocketErrorCode);
                e.Result = CLIENT_ERROR_RESULT;
                return;
            }

            catch (Exception ex)
            {
                Trace.TraceError("Exception in MouseWorker_DoWork(). Stack trace:\n{0}\n", ex.StackTrace);
                e.Result = CLIENT_ERROR_RESULT;
                return;
            }
        }

        private void mouseWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (innerState == InnerState.STOP_BY_ERROR || innerState == InnerState.STOP_BY_NETWORK_ERROR || innerState == InnerState.STOP_BY_CONTROLLER)
            { return; }

            if (e.Result != null)
            {
                string result = e.Result as string;
                switch (result)
                {
                    case NETWORK_ERROR_RESULT:
                        innerState = InnerState.STOP_BY_NETWORK_ERROR;
                        if (controlConnectionListener.IsBusy) { controlConnectionListener.CancelAsync(); }
                        break;

                    case CLIENT_ERROR_RESULT:
                        innerState = InnerState.STOP_BY_ERROR;
                        if (controlConnectionListener.IsBusy) { controlConnectionListener.CancelAsync(); }
                        break;
                }
            }
        }

        protected void ListeningForMouse()
        {
            mouseWorker = new BackgroundWorker();
            mouseWorker.WorkerSupportsCancellation = true;
            mouseWorker.DoWork += MouseWorker_DoWork;
            mouseWorker.RunWorkerCompleted += mouseWorker_RunWorkerCompleted;
            mouseWorker.RunWorkerAsync();
        }

        protected void StopMouseListening()
        {
            mouseWorker.CancelAsync();
            if (mouseSocket != null)
            {
                mouseSocket.Close();
            }
        }
        #endregion

        #region stopMethods
        public void Stop()
        {
            try
            {
                innerState = InnerState.STOP_BY_USER;
                WriteMessage(stream, TipoComandoBytes.CLOSE_CONNECTION, 0, TipoComandoBytes.CLOSE_CONNECTION.Length);
                controlConnectionListener.CancelAsync();
            }
            catch (Exception ex)
            {
                /*non devo fare nulla perché sto chiudendo*/
                Trace.TraceError("Exception in controlConnectionListener_DoWork(). Stack trace:\n{0}\n", ex.StackTrace);
            }
        }
        #endregion

        #region metodiPrivati
        private void ReadMessage(Stream stream, byte[] messageByte, int offset, int size)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                while (offset < size)
                {
                    int byteLetti = stream.Read(messageByte, offset, size - offset);
                    if (byteLetti == 0)
                    {
                        SocketClosedException sce = new SocketClosedException();
                        throw new IOException("No data avaible to read", sce);
                    }
                    offset += byteLetti;
                }
            }
            else { throw new NetworkException(); }
        }
        private void WriteMessage(Stream stream, byte[] msg, int start, int dim)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                stream.Write(msg, start, dim);
            }
            else
            {
                throw new NetworkException();
            }
        }
        private void forceButtonUp()
        {
            keyboard.KeyPressUp((byte)KeyInterop.VirtualKeyFromKey(Key.LeftCtrl));
            keyboard.KeyPressUp((byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift));
            keyboard.KeyPressUp((byte)KeyInterop.VirtualKeyFromKey(Key.LeftAlt));
        }
        #endregion
    }

    public class UdpActivatedState : AbstractActivatedState
    {
        public UdpActivatedState(Connection connection, ConnectionParameter param, System.Net.Sockets.TcpClient tcpControlConnection)
            : base(connection, param, tcpControlConnection) { }
        protected override IConnectionState NextState(State state)
        {
            switch (state)
            {
                case State.CONNECTED:
                    return new UdpConnectedState(connection, param, tcpControlConnection);
                case State.DISCONNECTED:
                    return new UdpDisconnectedState(connection, param);
                default:
                    throw new ArgumentException("Next State non valido: " + state.ToString());
            }
        }
        protected override Socket CreateSocket()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(param.Address), param.UdpPort);
            s.Bind(ep);
            return s;
        }
        protected override void ReadMessageMouseSocket()
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                byte[] data = ReceiveMouseMessage();
                string command = Encoding.ASCII.GetString(data, 0, COMMAND_LENGHT);
                switch (command)
                {
                    case TipoComando.MOUSE_MOVE:
                        byte[] bInt = new byte[4];
                        Array.Copy(data, 6, bInt, 0, 4);
                        int x = BitConverter.ToInt32(bInt, 0);
                        int y = BitConverter.ToInt32(data, 10);
                        mouse.ExecuteMouseMove(x, y);
                        break;
                    case TipoComando.CLICK_LEFT_DOWN:
                        mouse.ExecuteMouseDownLeft();
                        break;
                    case TipoComando.CLICK_LEFT_UP:
                        mouse.ExecuteMouseUpLeft();
                        break;
                    case TipoComando.CLICK_RIGHT_DOWN:
                        mouse.ExecuteMouseDownRight();
                        break;
                    case TipoComando.CLICK_RIGHT_UP:
                        mouse.ExecuteMouseUpRight();
                        break;
                    case TipoComando.CLICK_MIDDLE_DOWN:
                        mouse.ExecuteMouseDownMiddle();
                        break;
                    case TipoComando.CLICK_MIDDLE_UP:
                        mouse.ExecuteMouseUpMiddle();
                        break;
                    case TipoComando.MOUSE_SCROLL:
                        int delta = BitConverter.ToInt32(data, 6);
                        mouse.ExecuteMouseWeel(delta);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                throw new NetworkException();
            }
        }
        private byte[] ReceiveMouseMessage()
        {
            byte[] buffer = new byte[256];
            mouseSocket.Receive(buffer);
            return buffer;
        }
    }

    public class TcpActivatedState : AbstractActivatedState
    {
        public TcpActivatedState(Connection connection, ConnectionParameter param, System.Net.Sockets.TcpClient tcpControlConnection)
            : base(connection, param, tcpControlConnection) { }

        protected override IConnectionState NextState(State state)
        {
            switch (state)
            {
                case State.CONNECTED:
                    return new TcpConnectedState(connection, param, tcpControlConnection);
                case State.DISCONNECTED:
                    return new TcpDisconnectedState(connection, param);
                default:
                    throw new ArgumentException("Next State non valido: " + state.ToString());
            }
        }
        protected override Socket CreateSocket()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int portMouse = param.TcpPort + 2;
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(param.Address), portMouse);
            serverSocket.Bind(ep);
            serverSocket.Listen(10);
            Socket clientSocket = serverSocket.Accept();
            serverSocket.Close();
            return clientSocket;
        }

        protected override void ReadMessageMouseSocket()
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                byte[] data = new byte[256];
                int rec = 0;
                while ((rec += mouseSocket.Receive(data, 6, 0)) < 6) ;
                string command = Encoding.ASCII.GetString(data, 0, COMMAND_LENGHT);
                switch (command)
                {
                    case TipoComando.MOUSE_MOVE:
                        byte[] bInt = new byte[4];
                        rec = 0;
                        while ((rec += mouseSocket.Receive(bInt, 4, 0)) < 4) ;
                        int x = BitConverter.ToInt32(bInt, 0);
                        rec = 0;
                        while ((rec += mouseSocket.Receive(bInt, 4, 0)) < 4) ;
                        int y = BitConverter.ToInt32(bInt, 0);
                        mouse.ExecuteMouseMove(x, y);
                        break;
                    case TipoComando.CLICK_LEFT_DOWN:
                        mouse.ExecuteMouseDownLeft();
                        break;
                    case TipoComando.CLICK_LEFT_UP:
                        mouse.ExecuteMouseUpLeft();
                        break;
                    case TipoComando.CLICK_RIGHT_DOWN:
                        mouse.ExecuteMouseDownRight();
                        break;
                    case TipoComando.CLICK_RIGHT_UP:
                        mouse.ExecuteMouseUpRight();
                        break;
                    case TipoComando.CLICK_MIDDLE_DOWN:
                        mouse.ExecuteMouseDownMiddle();
                        break;
                    case TipoComando.CLICK_MIDDLE_UP:
                        mouse.ExecuteMouseUpMiddle();
                        break;
                    case TipoComando.MOUSE_SCROLL:
                        byte[] bIntDelta = new byte[4];
                        rec = 0;
                        while ((rec += mouseSocket.Receive(bIntDelta, 4, 0)) < 4) ;
                        int delta = BitConverter.ToInt32(bIntDelta, 0);
                        mouse.ExecuteMouseWeel(delta);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                throw new NetworkException();
            }
        }
    }

}
