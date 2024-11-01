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
        TcpClient tcpClient = new TcpClient();
        public static NetworkStream stream;

        public static TextBox ChatReadOnly;
        public static ComboBox ComboBoxClientsForDoc;

        // Track selected client and the open chat windows
        private string selectedClient = null;
        private Dictionary<string, ClientChatWindow> openClientWindows = new Dictionary<string, ClientChatWindow>();

        public int resistance = 0;
        public DoctorMainWindow(TcpClient client, NetworkStream networkStream)
        {
            InitializeComponent();
            this.tcpClient = client;
            stream = networkStream;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            chatReadOnly.ScrollToEnd();

        }

        // Event handler for the "Send" button to broadcast a message
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

        // Async broadcast message to all connected clients
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
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string encryptedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                string message = EncryptHelper.Decrypt(encryptedMessage);

                Console.WriteLine("Received message from server: " + message);

                if (message.StartsWith("clients_update:"))
                {
                    // Handle client list update (combobox)
                    string clientsList = message.Replace("clients_update:", "");
                    UpdateClientList(clientsList.Split(','));
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
                    } else
                    {
                        // Notify doctor of new message if no chat window is open
                        NotifyDoctorOfNewMessage(senderClient, clientMessage);
                    }
                }
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
                OpenClientChatWindow(clientName);
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
                        ComboBoxClientsForDoc.Items.Add(client);
                    }
                }
            });
        }

        // When doc clicks a client in the combobox, open a chatscreen
        private void CmbClientsForDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => ListenForUpdates());

            ChatReadOnly = chatReadOnly;
            ComboBoxClientsForDoc = CmbClientsForDoc;
        }

        private void chatBar_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click_ResistanceMin(object sender, RoutedEventArgs e)
        {
            resistance--;
            resistanceInputField.Text = resistance.ToString();
            SendMessageToClient(selectedClient, "Resistance changed to " + resistance.ToString());
        }

        private void Button_Click_ResistancePlus(object sender, RoutedEventArgs e)
        {
            resistance++;
            resistanceInputField.Text = resistance.ToString();
            SendMessageToClient(selectedClient, "Resistance changed to " + resistance.ToString());

        }

        private void Key_Down_ResistanceInputField(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                resistance = Int32.Parse(resistanceInputField.Text);
                SendMessageToClient(selectedClient, "Resistance changed to " + resistance.ToString());
            }
        }
    }
}
