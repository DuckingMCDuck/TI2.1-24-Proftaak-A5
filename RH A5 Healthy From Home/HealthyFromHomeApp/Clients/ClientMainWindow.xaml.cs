using Avans.TI.BLE;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HealthyFromHomeApp.Common;

namespace HealthyFromHomeApp.Clients
{
    public partial class ClientMainWindow : Window
    {
        internal static ClientMainWindow client; 

        internal string debugText
        {
            get
            {
                string stringReturn = "";
                Dispatcher.Invoke(() => { stringReturn = TextBoxBikeData.Text.ToString(); });
                return stringReturn;
            }
            set { Dispatcher.Invoke(() => { TextBoxBikeData.AppendText(value); }); }
        }
        internal string chatText
        {
            get
            {
                string stringReturn = "";
                Dispatcher.Invoke(() => { stringReturn = TextChat.Text.ToString(); });
                return stringReturn;
            }
            set { Dispatcher.Invoke(() => { TextChat.AppendText(value); }); }
        }

        // Publics:
        public TcpClient tcpClient;
        public static List<Tuple<string, byte[]>> sessionData = new List<Tuple<string, byte[]>>();
        public static Simulator simulator = new Simulator();
        public NetworkStream stream;

        // Privates:
        private static bool sessionRunning = false;
        private static bool debugScrolling = true;
        private static bool simulating = false;
        private string clientName;
        private static bool isReconnecting = false;

        // Toolbox-Items:
        public static TextBox TextBoxBikeData;
        public TextBox TextChat;

        public ClientMainWindow()
        {
            InitializeComponent();
            client = this; 
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxBikeData = TxtBikeData;
            TextChat = TxtChat;

            Task.Run(() => UsingSimulator());
        }

        private async Task UsingSimulator()
        {
            await Dispatcher.InvokeAsync(() => TextChat.AppendText("Using Simulator!\n"));

            ToggleSimulator();
        }

        private void ToggleSimulator()
        {
            Dispatcher.Invoke(() => TextChat.AppendText($"Simulator turned {(simulating ? "ON" : "OFF")}!\n"));

            if (!simulating)
            {
                Task.Run(() =>
                {
                    StartSimulator();
                });
            }
        }

        private void StartSimulator()
        {
            while (simulating)
            {
                simulator.SimulateData();
                client.Dispatcher.Invoke(() => SendToSimulator(client.debugText));
                Thread.Sleep(100); 
            }
        }

        private async void SendToSimulator(string debugText)
        {
            try
            {
                if (tcpClient == null || !tcpClient.Connected)
                {
                    await Dispatcher.InvokeAsync(() => TextChat.AppendText("Attempting to reconnect to the server...\n"));
                    await ReconnectToServerAsync();
                }

                if (stream.CanWrite)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(debugText);
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    await stream.FlushAsync();
                }
            }
            catch (SocketException ex)
            {
                await Dispatcher.InvokeAsync(() => TextChat.AppendText($"Socket error: {ex.Message}\n"));
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => TextChat.AppendText($"Error: {ex.Message}\n"));
            }
        }

        private async Task ReconnectToServerAsync()
        {
            try
            {
                if (isReconnecting) return; 
                isReconnecting = true;

                tcpClient?.Close();
                tcpClient = new TcpClient();

                await tcpClient.ConnectAsync("localhost", 12345);
                stream = tcpClient.GetStream();

                await Dispatcher.InvokeAsync(() => TextChat.AppendText("Reconnected successfully.\n"));
                isReconnecting = false;
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => TextChat.AppendText($"Reconnection failed: {ex.Message}\n"));
            }
        }

        private async void ListenForMessages()
        {
            byte[] buffer = new byte[1500];

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        await Dispatcher.InvokeAsync(() => TextChat.AppendText("Disconnected from the server.\n"));
                        break;
                    }

                    string encryptedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    string message = EncryptHelper.Decrypt(encryptedMessage); 

                    string[] splitMessage = message.Split(':');
                    if (splitMessage.Length > 1)
                    {
                        string sender = splitMessage[0].Trim();
                        string receivedMessage = message.Substring(sender.Length + 1).Trim();

                        await Dispatcher.InvokeAsync(() => TextChat.AppendText($"{sender}: {receivedMessage}\n"));
                    }
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => TextChat.AppendText($"Error: {ex.Message}\n"));
            }
        }

        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (clientName == null)
            {
                MessageBox.Show("You are not connected yet. Please connect before sending a message.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            string message = "chat:send_to:Doctor:" + TxtTypeBar.Text;
            string rawMessage = TxtTypeBar.Text;
            string rawClient = clientName.Substring("client:".Length);

            if (!string.IsNullOrEmpty(message))
            {
                string encryptedMessage = EncryptHelper.Encrypt(message);
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                TxtChat.AppendText($"{rawClient}: {rawMessage}\n");
                TxtTypeBar.Clear();
                SendMessageToServer(encryptedMessage);
            }
        }

        private async void SendMessageToServer(string message)
        {
            if (stream.CanWrite)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                clientName = "client:" + TxtName.Text;

                if (!string.IsNullOrEmpty(clientName))
                {
                    if (tcpClient == null || !tcpClient.Connected)
                    {
                        tcpClient = new TcpClient();
                        await tcpClient.ConnectAsync("localhost", 12345); 
                        stream = tcpClient.GetStream(); 
                    }

                    if (stream != null && stream.CanWrite)
                    {
                        string encryptedName = EncryptHelper.Encrypt(clientName);
                        byte[] nameData = Encoding.ASCII.GetBytes(encryptedName);
                        await stream.WriteAsync(nameData, 0, nameData.Length);
                        stream.Flush();

                        TxtChat.AppendText("Connected as: " + clientName + "\n");

                        Task.Run(() => ListenForMessages());
                    }
                    else
                    {
                        TxtChat.AppendText("Unable to write to the server stream.\n");
                    }
                }
                else
                {
                    TxtChat.AppendText("Please enter a valid name before connecting.\n");
                }
            }
            catch (SocketException ex)
            {
                TxtChat.AppendText($"Socket error: {ex.Message}\n");
            }
            catch (Exception ex)
            {
                TxtChat.AppendText($"Error: {ex.Message}\n");
            }
        }


        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            simulating = !simulating;
            ToggleSimulator();
        }

        private void TxtChat_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtChat.ScrollToEnd();
        }

        private void TxtBikeData_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            debugScrolling = !debugScrolling;
        }

        private void TxtBikeData_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (debugScrolling)
                TxtBikeData.ScrollToEnd();
            if (TxtBikeData.LineCount > 300)
                TxtBikeData.Text = TxtBikeData.Text.Substring(TxtBikeData.LineCount - 100);
        }
    }
}
