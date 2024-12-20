using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using HealthyFromHomeApp.Common;

namespace HealthyFromHomeApp.Doctor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DoctorMainWindow : Window
    {
        // Fields for TCP client connection and network stream for communication
        TcpClient tcpClient = new TcpClient();
        public static NetworkStream stream;

        // UI elements to display chat and list of connected clients
        public static TextBox ChatReadOnly;
        public static ComboBox ComboBoxClientsForDoc;

        // Track selected client and the open chat windows
        private string selectedClient = null;
        private Dictionary<string, ClientChatWindow> openClientWindows = new Dictionary<string, ClientChatWindow>();

        public int resistance = 0;

        // Constructor initializing TCP client and network stream, set up UI components
        private Dictionary<string, StringBuilder> fileContentBuffers = new Dictionary<string, StringBuilder>();

        public DoctorMainWindow(TcpClient client, NetworkStream networkStream)
        {
            InitializeComponent();
            this.tcpClient = client;
            stream = networkStream;
        }

        // Event handler to auto-scroll chat window to show the latest message
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            chatReadOnly.ScrollToEnd();

        }

        // Event handler for the "Send" button to broadcast a message to all connected clients
        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = chatBar.Text;

            if (!string.IsNullOrEmpty(message))
            {
                BroadcastMessage(message);
                ChatReadOnly.AppendText($"Doctor (Broadcast): {message}\n");
                chatBar.Clear();
            }
        }

        // Async method to broadcast message to all clients
        private async void BroadcastMessage(string message)
        {
            if (tcpClient.Connected)
            {
                string packet = $"broadcast:{message}";
                string encryptedPacket = EncryptHelper.Encrypt(packet);
                byte[] data = Encoding.ASCII.GetBytes(encryptedPacket);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush();
            }
        }

        // Async method to send message to a specific client
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

        // Continuously listen for updates from the server
        private async void ListenForUpdates()
        {
            byte[] buffer = new byte[1500];

            while (true)
            {
                // Read incoming data from the server
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string encryptedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                string message = EncryptHelper.Decrypt(encryptedMessage);

                Console.WriteLine("Received message from server: " + message);

                // Update the list of connected clients
                if (message.StartsWith("clients_update:"))
                {
                    // Handle client list update (combobox)
                    string clientsList = message.Replace("clients_update:", "");
                    UpdateClientList(clientsList.Split(','));
                }
                else if (message.Contains(":file_chunk:"))
                {
                    // Handle incoming file chunk
                    HandleFileChunk(message);
                }
                else if (message.Contains(":file_transfer_complete"))
                {
                    // Complete the file transfer and display accumulated content
                    CompleteFileTransfer(message);
                }
                else if (message.Contains("bike_data:"))
                {
                    // Handle incoming bike data and call AppendBikeData method
                    string[] messageParts = message.Split(':');
                    string clientName = messageParts[0]; // Assuming the format is "name:bike_data:clientName:data"
                    string bikeData = string.Join(":", messageParts.Skip(2));

                    // Check if there is an open chat window for the client
                    if (openClientWindows.ContainsKey(clientName))
                    {

                        Console.WriteLine("in Bike_Data");
                        // Forward the bike data to the specific ClientChatWindow instance
                        Dispatcher.Invoke(() => openClientWindows[clientName].AppendBikeData(bikeData));
                    }
                }
                else
                {
                    // Handle incoming messages from specific client
                    string[] messageParts = message.Split(':');
                    string senderClient = messageParts[0];
                    string clientMessage = string.Join(":", messageParts.Skip(1));

                    // If there's a chat window open for the client, add message
                    if (openClientWindows.ContainsKey(senderClient))
                    {
                        Dispatcher.Invoke(() => openClientWindows[senderClient].AppendMessage(clientMessage));
                    }
                    else
                    {
                        // Notify doctor of new message if no chat window is open
                        NotifyDoctorOfNewMessage(senderClient, clientMessage);
                    }
                }
            }
        }

        private void HandleFileChunk(string message)
        {
            // Parse the message to get client name and chunk content
            string[] parts = message.Split(new[] { ':' }, 4); // Split into [clientName, "file_chunk", chunkNumber, chunkContent]
            if (parts.Length < 4) return;

            string clientName = parts[1];
            string chunkContent = parts[3];

            // Initialize or append to the file content buffer for this client
            if (!fileContentBuffers.ContainsKey(clientName))
            {
                fileContentBuffers[clientName] = new StringBuilder();
            }
            fileContentBuffers[clientName].Append(chunkContent);
        }

        private void CompleteFileTransfer(string message)
        {
            string[] parts = message.Split(':');
            string clientName = parts[0];

            if (fileContentBuffers.ContainsKey(clientName))
            {
                // Retrieve the complete content for this client
                string completeContent = fileContentBuffers[clientName].ToString();

                // Display in ClientInfoTextBlock
                Dispatcher.Invoke(() =>
                {
                    ClientInfoTextBlock.Text = completeContent;
                });

                // Clear the buffer for the client as file transfer is complete
                fileContentBuffers.Remove(clientName);
            }
        }

        private void NotifyDoctorOfNewMessage(string clientName, string message)
        {
            MessageBoxResult result = MessageBox.Show(
                $"You have received a message from {clientName}: {message}\n\nWould you like to open the chat window?",
                "New Message Notification",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                OpenClientChatWindow(clientName); // Open a new chat window if doctor selects "Yes"
            }
        }

        // Update client list in the UI with the list received from the server
        private void UpdateClientList(string[] clients)
        {
            Dispatcher.Invoke(() =>
            {
                ComboBoxClientsForDoc.Items.Clear();
                foreach (string client in clients)
                {
                    if (!string.IsNullOrEmpty(client))
                    {
                        ComboBoxClientsForDoc.Items.Add(client); // Add each client to dropdown
                    }
                }
            });
        }

        // When doc clicks a client in the combobox, open a chatscreen
        public void CmbClientsForDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedClient = (string)CmbClientsForDoc.SelectedItem;
            if (selectedClient != null)
            {
                OpenClientChatWindow(selectedClient);
            }
        }

        // Open a chat window with specific client
        private void OpenClientChatWindow(string client)
        {
            if (!openClientWindows.ContainsKey(client))
            {
                // If no chat window is open, create one
                Dispatcher.Invoke(() =>
                {
                    ClientChatWindow chatWindow = new ClientChatWindow(client, tcpClient, stream);
                    chatWindow.Show();
                    chatWindow.Closed += (sender, args) => CloseClientChatWindow(client);
                    openClientWindows.Add(client, chatWindow);
                });
            }
            else
            {
                // If there is a window, bring it to the front
                Dispatcher.Invoke(() =>
                {
                    openClientWindows[client].Activate();
                });
            }
        }

        // Event handler for clicking the client combo box to open chat
        private void CmbClientsForDoc_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CmbClientsForDoc.SelectedItem != null)
            {
                selectedClient = (string)CmbClientsForDoc.SelectedItem;
                OpenClientChatWindow(selectedClient);
            }
        }

        public async void RequestFileFromServer(string clientName)
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                string packet = $"request_file:{clientName}";
                string encryptedPacket = EncryptHelper.Encrypt(packet);
                byte[] data = Encoding.ASCII.GetBytes(encryptedPacket);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush();
            }
        }

        private void HistoryDataOfClient_Click(object sender, RoutedEventArgs e)
        {
            RequestFileFromServer(selectedClient);
        }
        private void CloseClientChatWindow(string client)
        {
            if (openClientWindows.ContainsKey(client))
            {
                openClientWindows.Remove(client);
            }
        }
        private void chatBar_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => ListenForUpdates());

            ChatReadOnly = chatReadOnly;
            ComboBoxClientsForDoc = CmbClientsForDoc;
        }
    }
}
