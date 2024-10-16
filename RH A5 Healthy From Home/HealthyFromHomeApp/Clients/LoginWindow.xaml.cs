using HealthyFromHomeApp.Common;
using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace HealthyFromHomeApp.Clients
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string clientName = TxtClientName.Text;

            if (string.IsNullOrWhiteSpace(clientName))
            {
                MessageBox.Show("Please enter your name.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                TcpClient tcpClient = new TcpClient();
                await tcpClient.ConnectAsync("localhost", 12345);
                NetworkStream stream = tcpClient.GetStream();

                string message = "client:" + clientName;
                string encryptedMessage = EncryptHelper.Encrypt(message);
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush();

                ClientMainWindow mainWindow = new ClientMainWindow(message, tcpClient, stream);
                mainWindow.Show();
                this.Close();
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Failed to connect to server: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
