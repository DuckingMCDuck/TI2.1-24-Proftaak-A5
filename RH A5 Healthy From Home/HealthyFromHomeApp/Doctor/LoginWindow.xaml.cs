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

        // Event handler for loginbutton click
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Grab username and password
            string username = usernameBox.Text;
            string password = passwordBox.Password;

            // Validate both username and password provided
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            // Establish a TCP connection to server
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("localhost", 12345); // Connect Async
            stream = tcpClient.GetStream(); // Get stream for communication

            // Prepare packet
            string loginPacket = $"login:{username}:{password}";
            string encryptedLoginPacket = EncryptHelper.Encrypt(loginPacket);
            byte[] data = Encoding.ASCII.GetBytes(encryptedLoginPacket);

            // Send encrypted login packet to the server
            await stream.WriteAsync(data, 0, data.Length);
            stream.Flush();

            // Receive the servers response
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string encryptedResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            string response = EncryptHelper.Decrypt(encryptedResponse);

            // Handle the server's response to determine login success or failure
            if (response == "login_success")
            {
                MessageBox.Show("Login successful!");

                // Open the doctor's main window and close the login window
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
