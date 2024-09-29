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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DokterClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient tcpClient = new TcpClient();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //is de read only box, hierin moet de chats weergegeven worden, chat logica moet nog wordnen toegevoegd.
            chatReadOnly.ScrollToEnd();
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            //hoeft in principe niks mee te gebeuren.
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = chatBar.Text;
            string selectedClient = (string)CmbClientsForDoc.SelectedItem;
            if (!string.IsNullOrEmpty(message))
            {
                SendMessageToClient(selectedClient, message); 
                chatBar.Clear();
            }
        }

        private async void SendMessageToClient(string client, string message)
        {
            using (NetworkStream stream = tcpClient.GetStream())
            {
                string packet = $"send_to:{client}:{message}";
                byte[] data = Encoding.ASCII.GetBytes(packet);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        private async void ListenForUpdates()
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[1500];

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (message.StartsWith("clients_update:"))
                {
                    string clientsList = message.Replace("clients_updates", "");
                    UpdateClientList(clientsList.Split(','));
                }
                else
                {
                    Dispatcher.Invoke(()  => chatReadOnly.AppendText(message + "\n"));
                }
            }
        }

        private void UpdateClientList(string[] clients)
        {
            Dispatcher.Invoke(() =>
            {
                CmbClientsForDoc.Items.Clear();
                foreach (string client in clients)
                {
                    CmbClientsForDoc.Items.Add(client);
                }
            });
        }

        private void CmbClientsForDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
