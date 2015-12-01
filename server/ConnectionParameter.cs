using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerWPF
{
    public class ConnectionParameter
    {
        public string ServerName { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public short TcpPort { get; set; }
        public short UdpPort { get; set; }
        public bool UdpEnabled { get; set; }
        public bool PasswordEnabled { get; set; }

        private System.Configuration.Configuration config;

        public ConnectionParameter()
        {
            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ServerName = config.AppSettings.Settings["nomeServer"].Value;
            Password = config.AppSettings.Settings["password"].Value;
            if (!String.IsNullOrEmpty(config.AppSettings.Settings["indirizzo"].Value))
            {
                Address = config.AppSettings.Settings["indirizzo"].Value;
            }
            else
            {
                config.AppSettings.Settings["indirizzo"].Value = LocalIPAddressName();
                Address = config.AppSettings.Settings["indirizzo"].Value;
            }

            TcpPort = Int16.Parse(config.AppSettings.Settings["portTCP"].Value);
            UdpPort = Int16.Parse(config.AppSettings.Settings["portUDP"].Value);
            UdpEnabled = Boolean.Parse(config.AppSettings.Settings["enableUDP"].Value);
            PasswordEnabled = Boolean.Parse(config.AppSettings.Settings["passwordEnabled"].Value);
        }

        public void applyConfiguration()
        {
            config.AppSettings.Settings["nomeServer"].Value = ServerName;
            config.AppSettings.Settings["password"].Value = Password;
            config.AppSettings.Settings["indirizzo"].Value = Address;
            config.AppSettings.Settings["portTCP"].Value = TcpPort.ToString();
            config.AppSettings.Settings["portUDP"].Value = UdpPort.ToString();
            config.AppSettings.Settings["enableUDP"].Value = UdpEnabled.ToString();
            config.AppSettings.Settings["passwordEnabled"].Value = PasswordEnabled.ToString();
        }

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
    }
}
