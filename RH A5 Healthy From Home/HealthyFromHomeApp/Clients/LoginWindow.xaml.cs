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
            // Retrieve client name from input text box
            string clientName = TxtClientName.Text;

            // Validate name
            if (string.IsNullOrWhiteSpace(clientName))
            {
                // Show a warning message if no name is provided
                MessageBox.Show("Please enter your name.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Try connecting using TCP Client
                TcpClient tcpClient = new TcpClient();
                await tcpClient.ConnectAsync("localhost", 12345);
                NetworkStream stream = tcpClient.GetStream();

                // Prepare a message to send to the server with client's name
                string message = "client:" + clientName;
                string encryptedMessage = EncryptHelper.Encrypt(message);
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush();

                // Open the main client window after a succesful connection
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
