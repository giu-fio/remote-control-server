using System;
using System.Collections.Generic;
using System.Configuration;
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
    /// Interaction logic for requestPassword.xaml
    /// </summary>
    public partial class requestPassword : Window
    {

        System.Configuration.Configuration config;

        public requestPassword(Configuration config)
        {

            this.config = config; 
            InitializeComponent();
            
        }

        private void conferma_Click(object sender, RoutedEventArgs e)
        {
            confermaPassword();
        }

        private void passwordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                confermaPassword();
                
            }
        }

        private void confermaPassword()
        {
            if (!String.IsNullOrEmpty(passwordTextBox.Password) && passwordTextBox.Password.Equals(config.AppSettings.Settings["password"].Value))
            {
                MessageBox.Show("Password Corretta!");
                this.DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Password Errata");
                // this.DialogResult = false;
            }
        }

      
    }
}
