using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ServerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        #region variabili_Utili_Finestra
        //faccio un vettore di 3 posizioni dove:
        // errorValidation[0] --> errore nella porta TCP
        // errorValidation[1] --> errore nella porta UDP
        // errorValidation[2] --> errore nell'immissione nuova PASSWORD
        // errorValidation[3] --> errore nell'immissione nuova PASSWORD
        private bool[] errorValidation = new bool[4];
        private bool passwordChanged;
        private bool isConfigured;
        private bool isConnected;
        private bool isServerActive;
        private System.Windows.Forms.NotifyIcon notifyServer;
        private String path = "";
        private ConnectionParameter connectionParameter;

        private IConnection connection;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private StatusWindow status;
        private int contSec;
        private int contMin;
        private int contHour;

        #endregion

        #region COSTANTI_COMANDI_CLIENT

        private const string NON_CONNESSO = "Non connesso";
        private const string CONNESSO = "Connesso";
        private const string ATTIVO = "Attivo";

        #endregion

        public ServerWindow()
        {
            InitializeComponent();
            Closing += ServerWindow_Closing;
            isConfigured = false;
            isConnected = false;
            isServerActive = false;
            passwordChanged = false;
            notifyServer = new System.Windows.Forms.NotifyIcon();
            notifyServer.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyServer_mouseClickEvent);
            notifyServer.MouseMove += new System.Windows.Forms.MouseEventHandler(notifyServer_MouseMove);
            path = Directory.GetCurrentDirectory().Remove(Directory.GetCurrentDirectory().Length - 9);
            notifyServer.Icon = new System.Drawing.Icon(path + "\\Punto_rosso.ico");
            status = new StatusWindow(this);
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true; //cosi non compare l'icona
            notifyServer.Visible = true;
            notifyServer.BalloonTipTitle = "Server AVVIATO";
            notifyServer.BalloonTipText = "Caricate le impostazioni di default\nPremi START per mettere il server in attesa di connessioni";
            notifyServer.ShowBalloonTip(300);
            connectionParameter = new ConnectionParameter();
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            //questo metodo mi serve per inserire tutti gli indirizzi presente sulla macchina server
            caricaInfoIndirizzoServer();
            caricaInfoTextBox();
            stopButton.IsEnabled = false;
            stateLabel.Content = NON_CONNESSO;
            contHour = 0;
            contMin = 0;
            contSec = 0;
        }

        void ServerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            status.Close();
        }

        #region inizializzazione_Eventi
        private void inizializzaEventi()
        {
            connection.CloseConnection += connection_CloseConnection;
            connection.DisconnectClient += connection_DisconnectClient;
            connection.ConnectClient += connection_ConnectClient;
            connection.ActiveClient += connection_ActiveClient;
            connection.ClientError += connection_ClientError;
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        }

        private void deregistraEventi()
        {
            connection.CloseConnection -= connection_CloseConnection;
            connection.DisconnectClient -= connection_DisconnectClient;
            connection.ConnectClient -= connection_ConnectClient;
            connection.ActiveClient -= connection_ActiveClient;
            connection.ClientError -= connection_ClientError;
            dispatcherTimer.Tick -= dispatcherTimer_Tick;
        }

        void connection_CloseConnection(object sender, EventArgs e)
        {
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
            modificaButton.IsEnabled = true;
            stateLabel.Content = NON_CONNESSO;
            notifyServer.Icon = new System.Drawing.Icon(path + "\\Punto_rosso.ico");
            notifyServer.BalloonTipText = "In attesa di connessione";
            notifyServer.ShowBalloonTip(400);
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
                contSec = 0;
                contMin = 0;
                contHour = 0;
                timer.Content = "";
                isServerActive = false;
                isConnected = false;
            }
        }

        void connection_ClientError(object sender, ClientErrorEventArgs e)
        {
            switch (e.ErrorCode)
            {
                case ClientErrorEventArgs.CONNECTION_ERROR:
                    ErrorMessage("Impossibile stabilire una connessione\ncol client ");
                    break;
                case ClientErrorEventArgs.NETWORK_ERROR:
                    ErrorMessage(" Connessione assente!\nAssicurarsi di essere collegato alla rete ");
                    break;
                case ClientErrorEventArgs.CLIENT_ERROR:
                    ErrorMessage(" ATTENZIONE!\nIl client non risponde ai messaggi ");
                    break;
                default:
                    ErrorMessage(" ATTENZIONE!\nErrore inaspettato");
                    break;
            }
        }

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            contSec++;
            if (contSec == 60)
            {
                contSec = 0;
                contMin++;
                if (contMin == 60)
                {
                    contMin = 0;
                    contHour++;
                }
            }
            timer.Content = contHour.ToString("00") + ":" + contMin.ToString("00") + ":" + contSec.ToString("00");
        }

        private void connection_ActiveClient(object sender, EventArgs e)
        {
            stateLabel.Content = ATTIVO;
            notifyServer.Icon = new System.Drawing.Icon(path + "\\cerchio_verde.ico");
            notifyServer.BalloonTipText = "Controllato dal CLIENT";
            notifyServer.ShowBalloonTip(400);
            dispatcherTimer.Start();
            isServerActive = true;
        }

        private void connection_ConnectClient(object sender, ClientConnectionEventArgs e)
        {
            stateLabel.Content = CONNESSO;
            stopButton.IsEnabled = true;
            notifyServer.Icon = new System.Drawing.Icon(path + "\\cerchio_giallo.ico");
            notifyServer.BalloonTipText = "In attesa di interagire col CLIENT";
            notifyServer.ShowBalloonTip(400);
            isConnected = true;
        }

        private void connection_DisconnectClient(object sender, EventArgs e)
        {
            stateLabel.Content = NON_CONNESSO;
            stopButton.IsEnabled = true;
            notifyServer.Icon = new System.Drawing.Icon(path + "\\Punto_rosso.ico");
            notifyServer.BalloonTipText = "In attesa di connessione";
            notifyServer.ShowBalloonTip(400);
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
                contSec = 0;
                contMin = 0;
                contHour = 0;
                timer.Content = "";
                isServerActive = false;
                isConnected = false;
            }
        }

        #endregion

        #region funzioni_Caricamento

        private void caricaInfoIndirizzoServer()
        {
            IPHostEntry host;
            string localIP = null;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    localIP = ip.ToString();
                    addressTextBox.Items.Insert(0, ip.ToString());
                }
            }
        }

        private void caricaInfoTextBox()
        {
            passwordChanged = false;
            nameTextBox.Text = connectionParameter.ServerName;
            nameTextBox.IsEnabled = false;
            passwordTextBox.IsEnabled = false;
            checkBoxPassword.IsEnabled = false;
            if (connectionParameter.PasswordEnabled)
            {
                passwordTextBox.Password = connectionParameter.Password;
                checkBoxPassword.IsChecked = true;
                passwordVisibleButton.IsEnabled = true;
            }
            else
            {
                passwordTextBox.Password = "";
                checkBoxPassword.IsChecked = false;
                passwordVisibleButton.IsEnabled = false;
            }
            addressTextBox.Text = connectionParameter.Address;
            addressTextBox.IsEnabled = false;
            portTextBox.Text = connectionParameter.TcpPort.ToString();
            portTextBox.IsEnabled = false;
            portTextBoxUDP.IsEnabled = false;
            checkBoxEnableUdp.IsEnabled = false;
            ripristinaButton.IsEnabled = false;
            applyButton.IsEnabled = false;
            if (connectionParameter.UdpEnabled)
            {
                portTextBoxUDP.Text = connectionParameter.UdpPort.ToString();
                checkBoxEnableUdp.IsChecked = true;
                passwordVisibleButton.IsEnabled = true;
            }
            else
            {
                portTextBoxUDP.Text = "";
                checkBoxEnableUdp.IsChecked = false;
                passwordVisibleButton.IsEnabled = false;
            }
        }

        //metodo che mi restituisce l'indirizzo IP della macchina in cui gira il server
        private string LocalIPAddressName()
        {
            IPHostEntry host;
            string localIP = null;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        #endregion

        #region funzioni_Listener

        private void passwordVisibleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            passwordVisibleTextBox.Text = passwordTextBox.Password;
            passwordVisibleTextBox.Visibility = System.Windows.Visibility.Visible;
        }

        private void passwordVisibleButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            passwordVisibleTextBox.Visibility = System.Windows.Visibility.Hidden;
        }

        private void passwordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (passwordVisibleButton.Visibility == Visibility.Hidden)
            {
                passwordVisibleButton.Visibility = Visibility.Visible;
            }
            if (!applyButton.IsEnabled)
            {
                applyButton.IsEnabled = true;
            }
            if (!passwordChanged)
            {
                passwordChanged = true;
            }
        }

        private void passwordTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (passwordTextBox.Password.Length == 0)
            {
                passwordVisibleButton.Visibility = Visibility.Hidden;
            }
        }

        private void notifyServer_mouseClickEvent(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
                notifyServer.Visible = true;
            }
        }

        void notifyServer_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!notifyServer.Visible)
            {
                if (!isConnected && !isServerActive)
                {
                    //vuole dire che è solo configurato
                    notifyServer.BalloonTipText = "In attesa di connessione";
                    notifyServer.ShowBalloonTip(200);
                }
                else if (isConnected && !isServerActive)
                {
                    //vuole dire che è CONNESSO ma non attivo
                    notifyServer.BalloonTipText = "In attesa di interagire col CLIENT";
                    notifyServer.ShowBalloonTip(200);
                }
                else if (isConnected && isServerActive)
                {
                    //vuole dire che è ATTIVO
                    notifyServer.BalloonTipText = "ATTIVO da: " + timer.Content;
                    notifyServer.ShowBalloonTip(200);
                }
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {

            if (this.WindowState == WindowState.Normal)
            {
                notifyServer.Visible = false;
                this.ShowInTaskbar = true;
            }
            else if (this.WindowState == WindowState.Minimized)
            {
                if (!isConfigured)
                {
                    isConfigured = true;
                    this.ShowInTaskbar = false; //cosi non compare l'icona
                    notifyServer.Visible = true;
                    notifyServer.BalloonTipTitle = "Server CONFIGURATO";
                    notifyServer.BalloonTipText = "In attesa di connessione";
                    notifyServer.ShowBalloonTip(400);
                    isConfigured = true;
                }
                else
                {
                    if (!isConnected && !isServerActive)
                    {
                        this.ShowInTaskbar = false; //cosi non compare l'icona
                        notifyServer.Visible = true;
                        notifyServer.BalloonTipTitle = "Server CONFIGURATO";
                        notifyServer.BalloonTipText = "In attesa di connessione";
                        notifyServer.ShowBalloonTip(400);
                    }
                    else if (isConnected && !isServerActive)
                    {
                        this.ShowInTaskbar = false; //cosi non compare l'icona
                        notifyServer.Visible = true;
                        notifyServer.BalloonTipTitle = "Server CONNESSO";
                        notifyServer.BalloonTipText = "In attesa di interagire col CLIENT";
                        notifyServer.ShowBalloonTip(400);
                    }
                    else
                    {
                        this.ShowInTaskbar = false; //cosi non compare l'icona
                        notifyServer.Visible = true;
                        notifyServer.BalloonTipTitle = "Server ATTIVO";
                        notifyServer.BalloonTipText = "Controllato dal CLIENT";
                        notifyServer.ShowBalloonTip(400);
                    }
                }
            }
        }

        /* Metodi che abilitano il tasto APPLICA solo se modifico uno dei campi */
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            passwordVisibleButton.Visibility = Visibility.Hidden;
            if (!applyButton.IsEnabled)
            {
                applyButton.IsEnabled = true;
            }
        }

        private void addressTextBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            passwordVisibleButton.Visibility = Visibility.Hidden;
            if (!applyButton.IsEnabled)
            {
                applyButton.IsEnabled = true;
            }
        }

        private void preview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (passwordTextBox.IsEnabled)
            {
                passwordVisibleButton.Visibility = Visibility.Hidden;
            }
        }

        private void passwordTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (passwordTextBox.IsEnabled)
            {
                if (passwordTextBox.Password.Length > 0 && passwordVisibleButton.Visibility == Visibility.Hidden)
                {
                    passwordVisibleButton.Visibility = Visibility.Visible;
                }
            }
        }

        /* Metodo che applica e salva i valori dei campi nel file di configurazione */
        private void applyButton_Click(object sender, RoutedEventArgs e)
        {
            if (validateParameter())
            {
                String message = "Vuoi salvare le modifiche effettuate?";
                String caption = "Confirm";
                MessageBoxButton buttonsYN = MessageBoxButton.YesNo;
                MessageBoxResult resultMB = MessageBox.Show(message, caption, buttonsYN, MessageBoxImage.Exclamation);
                if (resultMB == MessageBoxResult.Yes)
                {
                    //allora applico le modifiche:
                    //disabilito i campi cosi che alla prossima apertura tutto ritorna allo stato iniziale
                    nameTextBox.IsEnabled = false;
                    passwordTextBox.IsEnabled = false;
                    addressTextBox.IsEnabled = false;
                    portTextBox.IsEnabled = false;
                    portTextBoxUDP.IsEnabled = false;
                    applyButton.IsEnabled = false;
                    modificaButton.IsEnabled = true;
                    ripristinaButton.IsEnabled = true;
                    checkBoxPassword.IsEnabled = false;
                    checkBoxEnableUdp.IsEnabled = false;
                    passwordVisibleButton.Visibility = Visibility.Hidden;
                    connectionParameter.ServerName = nameTextBox.Text;
                    if (checkBoxPassword.IsChecked.Value)
                    {
                        connectionParameter.PasswordEnabled = true;
                        connectionParameter.Password = passwordTextBox.Password;
                    }
                    else
                    {
                        connectionParameter.PasswordEnabled = false;
                        connectionParameter.Password = "";
                    }
                    
                    connectionParameter.TcpPort = Int16.Parse(portTextBox.Text);
                    connectionParameter.UdpPort = Int16.Parse(portTextBoxUDP.Text);
                    connectionParameter.Address = addressTextBox.Text;
                    //imposto i valori nel file di configurazione e salvo
                    connectionParameter.applyConfiguration();
                    passwordChanged = false;
                    //minimizzo la finestra e imposto il boolean isConfigured = true
                    isConfigured = true;
                    startButton.IsEnabled = true;
                }
            }
            else
            {
                String errorMsg = "";
                if (errorValidation[0])
                {
                    errorMsg += "Errore nell'immissione della porta TCP\n";
                }
                if (errorValidation[1])
                {
                    errorMsg += "Errore nell'immisione della porta UDP\n";
                }
                if (errorValidation[2])
                {
                    errorMsg += "Impossibile inserire la stessa password\n";
                }
                if (errorValidation[3])
                {
                    errorMsg += "Porte UDP e TCP uguali\n";
                }
                ErrorMessage(errorMsg);
                passwordChanged = false;
                for (int i = 0; i < 3; i++)
                {
                    errorValidation[i] = false;
                }
            }
        }

        // metodo che mi serve per capire se i parametri inseriti siano corretti e validi
        private bool validateParameter()
        {
            bool resultValidate = true;
            int port;
            if (!int.TryParse(portTextBox.Text, out port))
            {
                errorValidation[0] = true;
                resultValidate = false;
            }
            if (checkBoxEnableUdp.IsChecked.Value && !int.TryParse(portTextBoxUDP.Text, out port))
            {
                errorValidation[1] = true;
                resultValidate = false;
            }
            if (checkBoxEnableUdp.IsChecked.Value && int.TryParse(portTextBoxUDP.Text, out port) && int.TryParse(portTextBox.Text, out port))
            {
                if (int.Parse(portTextBoxUDP.Text) == int.Parse(portTextBox.Text))
                {
                    errorValidation[3] = true;
                    resultValidate = false;
                }
            }
            if (checkBoxPassword.IsChecked.Value && passwordChanged && connectionParameter.Password == passwordTextBox.Password)
            {
                errorValidation[2] = true;
                resultValidate = false;
            }

            return resultValidate;
        }

        /* Metodo che mi consente di modificare i vari campi, solo se viene immessa la password corretta */
        private void modificaButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;
            nameTextBox.IsEnabled = true;
            checkBoxPassword.IsEnabled = true;
            if (checkBoxPassword.IsChecked.Value)
            {
                passwordTextBox.IsEnabled = true;
                passwordVisibleButton.Visibility = Visibility.Visible;
            }
            else
            {
                passwordTextBox.IsEnabled = false;
                passwordVisibleButton.Visibility = Visibility.Hidden;
            }
            addressTextBox.IsEnabled = true;
            portTextBox.IsEnabled = true;
            modificaButton.IsEnabled = false;
            ripristinaButton.IsEnabled = true;
            checkBoxEnableUdp.IsEnabled = true;
            if (checkBoxEnableUdp.IsChecked.Value)
            {
                portTextBoxUDP.IsEnabled = true;
            }
            else
            {
                portTextBoxUDP.IsEnabled = false;
            }
        }

        /* Metodo che consente di inserire i valori di default..ovvero quei valori che si hanno quando si è aperta per la prima volta il server*/
        private void ripristinaButton_Click(object sender, RoutedEventArgs e)
        {
            caricaInfoTextBox();
            modificaButton.IsEnabled = true;
            ripristinaButton.IsEnabled = true;
            applyButton.IsEnabled = false;
            startButton.IsEnabled = true;
            passwordVisibleButton.Visibility = Visibility.Hidden;
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            connection = new Connection(connectionParameter);
            inizializzaEventi();
            status.Connection = connection;
            status.Show();
            ripristinaButton.IsEnabled = false;
            modificaButton.IsEnabled = false;
            applyButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
            passwordVisibleButton.Visibility = Visibility.Hidden;
            this.WindowState = WindowState.Minimized;
            connection.Start();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            connection.Stop();
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
            modificaButton.IsEnabled = true;
            passwordVisibleButton.Visibility = Visibility.Hidden;
            stateLabel.Content = NON_CONNESSO;
            notifyServer.Icon = new System.Drawing.Icon(path + "\\Punto_rosso.ico");
            notifyServer.BalloonTipText = "In attesa di connessione";
            notifyServer.ShowBalloonTip(400);
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
                contSec = 0;
                contMin = 0;
                contHour = 0;
                timer.Content = "";
                isServerActive = false;
                isConnected = false;
            }
            deregistraEventi();
        }

        private void checkBoxEnableUdp_Click(object sender, RoutedEventArgs e)
        {
            passwordVisibleButton.Visibility = Visibility.Hidden;
            if (!applyButton.IsEnabled)
            {
                applyButton.IsEnabled = true;
            }
            if (checkBoxEnableUdp.IsChecked.Value)
            {
                connectionParameter.UdpEnabled = true;
                portTextBoxUDP.IsEnabled = true;
                passwordVisibleButton.IsEnabled = true;
            }
            else
            {
                connectionParameter.UdpEnabled = false;
                portTextBoxUDP.IsEnabled = false;

                passwordVisibleButton.IsEnabled = false;
            }
        }

        private void checkBoxPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!applyButton.IsEnabled)
            {
                applyButton.IsEnabled = true;
            }
            if (checkBoxPassword.IsChecked.Value)
            {
                passwordTextBox.IsEnabled = true;
                if (passwordTextBox.Password.Length > 0)
                {
                    passwordVisibleButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                passwordTextBox.IsEnabled = false;
                passwordVisibleButton.Visibility = Visibility.Hidden;
                passwordTextBox.Password = "";
            }
        }

        #endregion

        #region metodiSupporto

        private void ErrorMessage(String message)
        {
            string caption = "ERROR!";
            MessageBoxButton buttons = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Error;
            MessageBox.Show(message, caption, buttons, icon);
        }

        #endregion



    }
}
