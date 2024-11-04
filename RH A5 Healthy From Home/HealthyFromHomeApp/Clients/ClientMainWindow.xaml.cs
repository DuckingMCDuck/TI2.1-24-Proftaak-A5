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
using BikeLibrary;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;
using Client;
using Client.Virtual_Reality;

namespace HealthyFromHomeApp.Clients
{
    public partial class ClientMainWindow : Window
    {
        private bool isSessionActive = false; // Track if a bike session is active

        internal static ClientMainWindow client;

        // Properties for getting and setting text to debug or chat textboxes
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
        public Simulator simulator;
        public NetworkStream stream;
        private VRServer vrServer;

        // Privates:
        private static bool sessionRunning = false;
        private static bool debugScrolling = true;
        private bool simulating = false;
        private string clientName;
        private static bool isReconnecting = false;
        private bool bikeConnected = false;
        private BikeSessionWindow currentBikeSession;

        // Toolbox-Items:
        public TextBox TextBoxBikeData;
        public TextBox TextChat;

        // Reference to bikehelper class
        private BikeHelper bikeHelper;

        // Constructor
        public ClientMainWindow(string clientName, TcpClient client, NetworkStream networkStream)
        {
            InitializeComponent();
            this.clientName = clientName;
            this.tcpClient = client;
            this.stream = networkStream;
            this.simulator = new Simulator(this);
            this.bikeHelper = new BikeHelper();
            this.vrServer = new VRServer(this);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxBikeData = TxtBikeData;
            TextChat = TxtChat;
            TxtChat.AppendText($"Connected as: {clientName}\n");

            await Task.Run(() => ListenForMessages());

            await Task.Run(() => VRServer.Start());
        }

        // Toggle the simulator on/off, Turning it on and off
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

        // Toggle simulator
        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            simulating = !simulating;
            ToggleSimulator();
        }

        // Start generating data, solely responsible for making data
        private void StartSimulator()
        {
            while (simulating)
            {
                simulator.SimulateData();
                Thread.Sleep(500);
            }
        }

        // Constant messagelistener
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

                    if (message.Contains("Resistance changed to "))
                    {
                        string[] parts = message.Split(' ');
                        BikeHelper.SendDataToBike(bikeHelper.BLE, int.Parse(parts[4]));

                    }

                        string[] splitMessage = message.Split(':');
                    if (splitMessage.Length > 1)
                    {
                        string sender = splitMessage[0].Trim();
                        string receivedMessage = message.Substring(sender.Length + 1).Trim();

                        if (receivedMessage == "start_session")
                        {
                            if (!bikeConnected)
                            {
                                SendMessageToServer("chat:send_to:Doctor:The bike is not connected.");
                            }
                            else if (currentBikeSession != null)
                            {
                                Dispatcher.Invoke(() => currentBikeSession.StartSession());
                                isSessionActive = true;
                            }
                        }
                        else if (receivedMessage == "stop_session")
                        {
                            if (currentBikeSession != null)
                            {
                                Dispatcher.Invoke(() => currentBikeSession.StopSession());
                                isSessionActive = false;
                            }
                        }
                        else if (receivedMessage == "start_heartrate")
                        {
                            if (currentBikeSession != null)
                            {
                                Dispatcher.Invoke(() => currentBikeSession.startHeartRateMonitor());
                            }
                        }
                        else
                        {
                            await Dispatcher.InvokeAsync(() => TextChat.AppendText($"{sender}: {receivedMessage}\n"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => TextChat.AppendText($"Error: {ex.Message}\n"));
            }
        }

        // Event handler for sending messages
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

            // Prepare message
            string message = "chat:send_to:Doctor:" + TxtTypeBar.Text;
            string rawMessage = TxtTypeBar.Text;
            string rawClient = clientName.Substring("client:".Length);

            string encryptedMessage = EncryptHelper.Encrypt(message);
            byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
            TxtChat.AppendText($"{rawClient}: {rawMessage}\n");
            TxtTypeBar.Clear();
            SendMessageToServer(encryptedMessage); // Send to server

        }

        // Helper method to send it to the server
        private async void SendMessageToServer(string message)
        {
            if (stream.CanWrite)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
        }

        // Event handler to connect to the bike
        private async void BtnConnectBike_Click(object sender, RoutedEventArgs e)
        {
            string enterdText = Interaction.InputBox("Enter the last 5 digits of the serial-number of the bike to continue: ", "Enter bike details", "");
            if (enterdText.Length > 5)
            {
                MessageBox.Show("This message is too long!");
                return;
            }
            string pattern = "[0-9]{5}";
            string match = Regex.Match(enterdText, pattern).Value;
            if (match == null || match == "")
            {
                MessageBox.Show("No bike details were specified, please try again.");
                return;
            }
            if (isSessionActive)
            {
                MessageBox.Show("A session is already running. Please stop the current session before starting a new one.", "Session Already Running", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TxtBikeStatus.Text = "Attempting to connect to the bike...\n";

            // List all available devices
            List<string> availableDevices = bikeHelper.GetAvailableDevices();
            TxtBikeStatus.Text += "Devices found:\n";
            foreach (var device in availableDevices)
            {
                TxtBikeStatus.Text += $"Device: {device}\n";
            }

            // Try to connect to specified bike (Todo: grab and use specified serialcode)
            bool bikeConnected = await bikeHelper.ConnectToBike("Tacx Flux " + match);
            if (bikeConnected)
            {
                TxtBikeStatus.Text += "Bike connected successfully!\n";
                this.bikeConnected = true;

                // Create and store reference to BikeSessionWindow
                currentBikeSession = new BikeSessionWindow(bikeHelper, tcpClient, clientName);
                currentBikeSession.Closed += (s, args) =>
                {
                    isSessionActive = false;
                    currentBikeSession = null;
                };
                currentBikeSession.Show();

                isSessionActive = true;
            }
            else
            {
                TxtBikeStatus.Text += "Failed to connect to the bike.\n";
            }
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
