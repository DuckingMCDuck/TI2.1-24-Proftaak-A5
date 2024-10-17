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
        private bool bikeConnected = false;

        // Toolbox-Items:
        public static TextBox TextBoxBikeData;
        public TextBox TextChat;

        public ClientMainWindow(string clientName, TcpClient client, NetworkStream networkStream)
        {
            InitializeComponent();
            this.clientName = clientName;
            this.tcpClient = client;
            this.stream = networkStream;
            ClientMainWindow.client = this;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxBikeData = TxtBikeData;
            TextChat = TxtChat;
            TxtChat.AppendText($"Connected as: {clientName}\n");

            Task.Run(() => ListenForMessages());
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

            if (simulating)
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
                Thread.Sleep(500); 
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

            if (String.IsNullOrEmpty(TxtTypeBar.Text))
            {
                MessageBox.Show("Please enter a message before sending it.", "Message Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = "chat:send_to:Doctor:" + TxtTypeBar.Text;
            string rawMessage = TxtTypeBar.Text;
            string rawClient = clientName.Substring("client:".Length);

            string encryptedMessage = EncryptHelper.Encrypt(message);
            byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
            TxtChat.AppendText($"{rawClient}: {rawMessage}\n");
            TxtTypeBar.Clear();
            SendMessageToServer(encryptedMessage);
            
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

        private async void BtnConnectBike_Click(object sender, RoutedEventArgs e)
        {
            TxtBikeStatus.Text = "Attempting to connect to the bike...\n";
            bikeConnected = true;
            TxtBikeStatus.Text = "Bike connected successfully!\n";
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
