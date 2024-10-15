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

        private string selectedClient = null;
        private Dictionary<string, ClientChatWindow> openClientWindows = new Dictionary<string, ClientChatWindow>();
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
                    string clientsList = message.Replace("clients_update:", "");
                    UpdateClientList(clientsList.Split(','));
                }
                else
                {
                    string[] messageParts = message.Split(':');
                    string senderClient = messageParts[0];
                    string clientMessage = string.Join(":", messageParts.Skip(1));

                    if (openClientWindows.ContainsKey(senderClient))
                    {
                        Dispatcher.Invoke(() => openClientWindows[senderClient].AppendMessage(clientMessage));
                    }
                }
            }
        }

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

        private void CmbClientsForDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedClient = (string)CmbClientsForDoc.SelectedItem;
            if (selectedClient != null)
            {
                OpenClientChatWindow(selectedClient);
            }
        }

        private void OpenClientChatWindow(string client)
        {
            if (!openClientWindows.ContainsKey(client))
            {
                ClientChatWindow chatWindow = new ClientChatWindow(client, tcpClient, stream);
                chatWindow.Show();
                openClientWindows.Add(client, chatWindow);
            }
            else
            {
                openClientWindows[client].Activate();
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
    }
}
