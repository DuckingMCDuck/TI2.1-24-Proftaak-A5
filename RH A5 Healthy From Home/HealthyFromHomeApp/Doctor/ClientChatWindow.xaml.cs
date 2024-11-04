using HealthyFromHomeApp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public int resistance = 0;

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
        private void StartSessionButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessageToClient(clientName, "start_session");
        }

        private void StopSessionButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessageToClient(clientName, "stop_session");
        }

        private async void SendMessageToClient(string client, string message)
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                string packet = $"send_to:{client}:{message}";
                string encryptedPacket = EncryptHelper.Encrypt(packet);
                byte[] data = Encoding.ASCII.GetBytes(encryptedPacket);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush();
            }
            else
            {
                // Handle the case where tcpClient is null or not connected
            }
        }

        // Public method to add received messages to chat history
        public void AppendMessage(string message)
        {
            chatHistory.AppendText($"{clientName}:{message}\n");
        }

        public void AppendBikeData(string data)
        {
            Dispatcher.Invoke(() =>
            {
                bikeDataTextBox.Clear();
                bikeDataTextBox.AppendText($"{data}\n");
                bikeDataTextBox.ScrollToEnd();
            });
        }
        public void NotifyBikeNotConnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("The bike is not connected on the client side. Please check the client connection.", "Bike Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        private void Button_Click_ResistanceMin(object sender, RoutedEventArgs e)
        {
            resistance--;
            resistanceSlider.Value = resistance;
            SendMessageToClient(clientName, "Resistance changed to " + resistance.ToString());
        }

        private void Button_Click_ResistancePlus(object sender, RoutedEventArgs e)
        {
            resistance++;
            resistanceSlider.Value = resistance;
            SendMessageToClient(clientName, "Resistance changed to " + resistance.ToString());

        }

        private void Slider_ValueChanged(object sender, DragCompletedEventArgs e)
        {
            resistance = (int) resistanceSlider.Value;
            SendMessageToClient(clientName, "Resistance changed to " + resistance.ToString());
        }

        private void EmergencyStop_Click(object sender, RoutedEventArgs e)
        {
            resistance = 200;
            resistanceSlider.Value = resistance;
            SendMessageToClient(clientName, "Resistance changed to " + resistance.ToString());
            SendMessageToClient(clientName, "EMERGENCY STOP!");
        }
    }
}
