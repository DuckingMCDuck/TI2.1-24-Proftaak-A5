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

        // Event handler for the Send button to send a message to the client
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

        // Event handler to initiate a session by sending a start command to the client
        private void StartSessionButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessageToClient(clientName, "start_session");
        }

        // Event handler to stop the session by sending a stop command to the client
        private void StopSessionButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessageToClient(clientName, "stop_session");
        }

        // Method to send messages to the client with encryption for secure communication
        private async void SendMessageToClient(string client, string message)
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                string packet = $"send_to:{client}:{message}";
                string encryptedPacket = EncryptHelper.Encrypt(packet); // Encrypt the message
                byte[] data = Encoding.ASCII.GetBytes(encryptedPacket); // Convert to byte array
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush();
            }
            else
            {
                // Handle the case where tcpClient is null or not connected
            }
        }

        // Method to append a received message to the chat history in the UI
        public void AppendMessage(string message)
        {
            chatHistory.AppendText($"{clientName}:{message}\n");
        }

        // Method to update bike data in the UI's bike data text box
        public void AppendBikeData(string data)
        {
            Dispatcher.Invoke(() => // Ensure UI updates happen on the main thread
            {
                bikeDataTextBox.Clear();
                bikeDataTextBox.AppendText($"{data}\n");
                bikeDataTextBox.ScrollToEnd();
            });
        }

        // Method to notify the doctor if the bike is not connected on the client's side
        public void NotifyBikeNotConnected()
        {
            Dispatcher.Invoke(() => 
            {
                MessageBox.Show("The bike is not connected on the client side. Please check the client connection.", "Bike Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        // Event handler to decrease the bike's resistance level and notify the client
        private void Button_Click_ResistanceMin(object sender, RoutedEventArgs e)
        {
            resistance--; // Decrease resistance level
            resistanceSlider.Value = resistance; // Update the slider value
            SendMessageToClient(clientName, "Resistance changed to " + resistance.ToString());
        }

        // Event handler to increase the bike's resistance level and notify the client
        private void Button_Click_ResistancePlus(object sender, RoutedEventArgs e)
        {
            resistance++; // Increase resistance level
            resistanceSlider.Value = resistance; // Update the slider value
            SendMessageToClient(clientName, "Resistance changed to " + resistance.ToString());

        }

        // Event handler for when the resistance slider value is changed, sending the new value to the client
        private void Slider_ValueChanged(object sender, DragCompletedEventArgs e)
        {
            resistance = (int) resistanceSlider.Value; // Set resistance to the slider's current value
            SendMessageToClient(clientName, "Resistance changed to " + resistance.ToString());
        }

    }
}
