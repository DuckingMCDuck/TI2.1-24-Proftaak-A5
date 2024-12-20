﻿using System;
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

namespace DokterClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient tcpClient = new TcpClient();
        public static NetworkStream stream;

        public static TextBox ChatReadOnly;
        public static ComboBox ComboBoxClientsForDoc;

        private string selectedClient = null;
        public MainWindow()
        {
            InitializeComponent();

            
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //is de read only box, hierin moet de chats weergegeven worden, chat logica moet nog worden toegevoegd.
            chatReadOnly.ScrollToEnd();

        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = chatBar.Text;

            if (!string.IsNullOrEmpty(message) && selectedClient != null)
            {
                SendMessageToClient(selectedClient, message); 
                ChatReadOnly.AppendText($"Doctor:{message}\n");
                chatBar.Clear();
            }
            else
            {
                ChatReadOnly.AppendText("Please select a client and enter a message.\n");
            } 
                
        }

        private async void SendMessageToClient(string client, string message)
        {
            if (tcpClient.Connected)
            {
                string packet = $"send_to:{client}:{message}";
                byte[] data = Encoding.ASCII.GetBytes(packet);
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
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Console.WriteLine("Received message from server: " + message);  

                if (message.StartsWith("clients_update:"))
                {
                    string clientsList = message.Replace("clients_update:", "");
                    UpdateClientList(clientsList.Split(','));  
                }
                else
                {
                    if (selectedClient != null && message.StartsWith($"{selectedClient}:"))
                    {
                        Dispatcher.Invoke(() => ChatReadOnly.AppendText(message + "\n"));
                    }
                }
            }
        }

        private void UpdateClientList(string[] clients)
        {
            Dispatcher.Invoke(() =>
            {
                CmbClientsForDoc.Items.Clear();
                Console.WriteLine("Updating ComboBox with clients: " + string.Join(", ", clients));

                foreach (string client in clients)
                {
                    if (!string.IsNullOrEmpty(client)) 
                    {
                        CmbClientsForDoc.Items.Add(client);
                    }
                }
                Console.WriteLine("ComboBox now contains " + CmbClientsForDoc.Items.Count + " clients.");

            });
        }

        private void CmbClientsForDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedClient = (string)CmbClientsForDoc.SelectedItem;
            ChatReadOnly.AppendText($"You are now talking to: {selectedClient}\n");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tcpClient.Connect("localhost", 12345);
            stream = tcpClient.GetStream();

            // Create public variables for items in the Toolbox (Schrijf volledig uit: Txt -> TextBox, Btn -> Button etc.)
            Task.Run(() => ListenForUpdates());

            ChatReadOnly = chatReadOnly;
            ComboBoxClientsForDoc = CmbClientsForDoc;
        }

        private void chatBar_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
