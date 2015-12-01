using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ServerWPF
{
    /// <summary>
    /// Logica di interazione per StatusWindow.xaml
    /// </summary>
    public partial class StatusWindow : Window
    {
        private IConnection connection;
        private Brush green;
        private Brush yellow;
        private Brush red;
        private ServerWindow mainWindow;


        public StatusWindow(ServerWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            var bc = new BrushConverter();
            green = (Brush)bc.ConvertFrom("#FF76FF03");
            yellow = (Brush)bc.ConvertFrom("#FFFFFF00");
            red = (Brush)bc.ConvertFrom("#FFF44336");
            Background = red;
            Deactivated += StatusWindow_Deactivated;

        }

        void StatusWindow_Deactivated(object sender, EventArgs e)
        {
            Window win = (Window)sender;
            win.Topmost = true;
        }

        public IConnection Connection
        {
            get { return connection; }
            set
            {
                this.connection = value;
                this.connection.ActiveClient += connection_ActiveClient;
                this.connection.ConnectClient += connection_ConnectClient;
                this.connection.DisconnectClient += connection_DisconnectClient;
                this.connection.ClientError += connection_ClientError;
                this.connection.CloseConnection += connection_CloseConnection;
            }
        }

        void connection_CloseConnection(object sender, EventArgs e)
        {
            Background = red;
        }

        void connection_ClientError(object sender, ClientErrorEventArgs e)
        {
            Background = red;
        }

        void connection_DisconnectClient(object sender, EventArgs e)
        {
            Background = red;
        }

        void connection_ConnectClient(object sender, ClientConnectionEventArgs e)
        {
            Background = yellow;
        }

        void connection_ActiveClient(object sender, EventArgs e)
        {
            Background = green;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            mainWindow.WindowState = WindowState.Normal;
        }

    }
}
