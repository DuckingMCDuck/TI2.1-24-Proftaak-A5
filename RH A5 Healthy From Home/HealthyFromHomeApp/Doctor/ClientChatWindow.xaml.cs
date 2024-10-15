using HealthyFromHomeApp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

namespace HealthyFromHomeApp.Doctor
{
    public partial class ClientChatWindow : Window
    {
        private readonly string clientName;
        private readonly TcpClient tcpClient;
        private readonly NetworkStream stream;

        public ClientChatWindow(string clientName, TcpClient client, NetworkStream networkStream)
        {
            InitializeComponent();
            this.clientName = clientName;
            this.tcpClient = client;
            this.stream = networkStream;
            this.Title = $"Chat with {clientName}";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = messageBox.Text;

            if (!string.IsNullOrEmpty(message))
            {
                SendMessageToClient(clientName, message); 
                chatHistory.AppendText($"Doctor: {message}\n");
                messageBox.Clear(); 
            }
        }

        private async void SendMessageToClient(string client, string message)
        {
            if (tcpClient.Connected)
            {
                string packet = $"send_to:{client}:{message}";
                string encryptedPacket = EncryptHelper.Encrypt(packet);
                byte[] data = Encoding.ASCII.GetBytes(encryptedPacket);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush();
            }
        }

        public void AppendMessage(string message)
        {
            chatHistory.AppendText($"{clientName}: {message}\n");
        }
    }
}
