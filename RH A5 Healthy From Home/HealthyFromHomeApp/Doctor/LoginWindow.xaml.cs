using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using HealthyFromHomeApp.Common;

namespace HealthyFromHomeApp.Doctor
{
    public partial class LoginWindow : Window
    {
        private TcpClient tcpClient;
        private NetworkStream stream;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameBox.Text;
            string password = passwordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("localhost", 12345);
            stream = tcpClient.GetStream();

            string loginPacket = $"login:{username}:{password}";
            string encryptedLoginPacket = EncryptHelper.Encrypt(loginPacket);
            byte[] data = Encoding.ASCII.GetBytes(encryptedLoginPacket);

            await stream.WriteAsync(data, 0, data.Length);
            stream.Flush();

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string encryptedResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            string response = EncryptHelper.Decrypt(encryptedResponse);

            if (response == "login_success")
            {
                MessageBox.Show("Login successful!");

                DoctorMainWindow mainWindow = new DoctorMainWindow(tcpClient, stream);  
                mainWindow.Show();
                this.Close(); 
            }
            else
            {
                MessageBox.Show("Invalid username or password.");
            }
        }
    }
}
