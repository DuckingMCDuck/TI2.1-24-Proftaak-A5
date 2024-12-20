using Avans.TI.BLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static MainWindow client; // MainWindow property to store the main window instance to access it from other classes

        //Properties to update TextBoxes(From another class)
        internal string debugText
        {
            get
            {
                string stringReturn = "";
                Dispatcher.Invoke(new Action(() => { stringReturn = TextBoxBikeData.Text.ToString(); }));
                return stringReturn;
            }
            set { Dispatcher.Invoke(new Action(() => { TextBoxBikeData.AppendText(value); })); }
        }
        internal string chatText
        {
            get {
                string stringReturn = "";
                Dispatcher.Invoke(new Action(() => { stringReturn = TextChat.Text.ToString(); }));
                return stringReturn;
            }
            set { Dispatcher.Invoke(new Action(() => { TextChat.AppendText(value); })); }
        }

        // Publics:
        public static TcpClient tcpClient = new TcpClient();
        public static List<Tuple<string, byte[]>> sessionData = new List<Tuple<string, byte[]>>();
        public static Simulator simulator = new Simulator();
        private static VRServer vrServer = new VRServer();
        public static NetworkStream stream;

        // Privates:
        private static bool sessionRunning = false;
        private static bool debugScrolling = true;
        private static bool simulating = true;
        private static string clientName;

        // Toolbox-Items:
        public static TextBox TextBoxBikeData;
        public static TextBox TextChat;

        public MainWindow()
        {
            Thread.Sleep(1000);
            InitializeComponent();
            client = this; // Initialize class 'Client' property
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to the server
            tcpClient.Connect("localhost", 12345);
            stream = tcpClient.GetStream();

            // Create public variables for items in the Toolbox (Schrijf volledig uit: Txt -> TextBox, Btn -> Button etc.)
            TextBoxBikeData = TxtBikeData;
            TextChat = TxtChat;

            // Start VR Server
            await VRServer.Start();

            #region Connecting via bike (FietsDemo code)
            //UsingBicycle();
            #endregion

            #region Connecting via Simulator
            // Start Simulator on a new Thread
            UsingSimulator();
            #endregion
        }

        /// <summary>
        /// Self-written Events:
        /// </summary>
        private static async void UsingBicycle()
        {
            int errorCode = 0;
            BLE bleBike = new BLE();
            BLE bleHeart = new BLE();
            Thread.Sleep(1000); // We need some time to list available devices

            // List available devices
            List<String> bleBikeList = bleBike.ListDevices();
            TextBoxBikeData.AppendText("Devices found: ");
            //TextBoxBikeData.AppendText("Devices found: ");
            foreach (var name in bleBikeList)
            {
                TextBoxBikeData.AppendText($"Device: {name}");
            }

            // Connect to bike using the last 5 digits of the serial number:
            errorCode = await bleBike.OpenDevice("Tacx Flux 00438");
            // __TODO__ Error check

            // Print availible services
            var services = bleBike.GetServices;
            foreach (var service in services)
            {
                TextBoxBikeData.AppendText($"Service: {service}");
            }

            // Set service
            errorCode = await bleBike.SetService("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
            // __TODO__ error check

            // Subscribe
            bleBike.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
            errorCode = await bleBike.SubscribeToCharacteristic("6e40fec2-b5a3-f393-e0a9-e50e24dcca9e");
            // __TODO__ error check

            // Heart rate
            errorCode = await bleHeart.OpenDevice("Decathlon Dual HR");
            // __TODO__ error check

            await bleHeart.SetService("HeartRate");

            bleHeart.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
            await bleHeart.SubscribeToCharacteristic("HeartRateMeasurement");

            TextChat.AppendText("Connected to Bike!");
        }

        private static void UsingSimulator()
        {
            // Update chat
            TextChat.AppendText("Using Simulator!\n");
            // Use Simulator
            simulating = false;
            ToggleSimulator();
        }

        private static void ToggleSimulator()
        {
            TextChat.AppendText("Simulator turned " + (simulating ? "ON" : "OFF") + "!\n");
            if (simulating)
            {
                Task.Run(() => 
                {
                    StartSimulator();
                });
            }
        }

        private static void StartSimulator()
        {
            // Simulating bike data
            while (simulating)
            {
                simulator.SimulateData();
                // Send DebugText to simulator
                client.Dispatcher.Invoke(() => {
                    SendToSimulator(client.debugText);
                });
            }
        }

        private static void SendToSimulator(string debugText)
        {
            try
            {
                // Check if the TcpClient is connected before trying to send data
                if (tcpClient == null || !tcpClient.Connected)
                {
                    // Attempt to reconnect if not connected
                    client.Dispatcher.Invoke(() => {
                        TextChat.AppendText("Attempting to reconnect to the server...\n");
                    });

                    // You may want to make this a proper async method with retries
                    tcpClient = new TcpClient();
                    tcpClient.Connect("localhost", 12345); // Use your appropriate IP and port
                    client.Dispatcher.Invoke(() => {
                        TextChat.AppendText("Reconnected successfully.\n");
                    });
                }

                // Now that we're sure the client is connected, get the network stream and send data
                if (stream.CanWrite)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(debugText);
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush(); // Ensure the data is sent
                }
            }
            catch (SocketException ex)
            {
                // Handle socket exception, possibly logging and attempting a reconnect
                client.Dispatcher.Invoke(() => {
                    TextChat.AppendText($"Socket error: {ex.Message}\n");
                });
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                client.Dispatcher.Invoke(() => {
                    TextChat.AppendText($"Error: {ex.Message}\n");
                });
            }
        }

        private static void BleBike_SubscriptionValueChanged(object sender, BLESubscriptionValueChangedEventArgs e)
        {
            if (sessionRunning == true)
            {
                // Print incoming data from bike
                client.Dispatcher.Invoke(() => {
                    TextBoxBikeData.AppendText($"Received from {e.ServiceName}: {BitConverter.ToString(e.Data).Replace("-", " ")}, {Encoding.UTF8.GetString(e.Data)}");
                });

                //TextBoxBikeData.AppendText($"Received from {e.ServiceName}: {BitConverter.ToString(e.Data).Replace("-", " ")}, {Encoding.UTF8.GetString(e.Data)}");

                // Add data to session
                sessionData.Add(new Tuple<string, byte[]>(e.ServiceName, e.Data));
            }
        }

        /// <summary>
        /// Generated Events:
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBikeData_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Show Bike/ Simulator data (Debug)
            if (debugScrolling)
                TxtBikeData.ScrollToEnd();
            // Keep only the recent data availible to scroll through
            if (TxtBikeData.LineCount > 300)
                TxtBikeData.Text = TxtBikeData.Text.Substring(TxtBikeData.LineCount - 100);
        }

        private void TxtChat_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Always scroll to end of chat
            TxtChat.ScrollToEnd();
        }

        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            // Get written message and send it to the chat (server)
            string message = "send_to:Doctor:"+TxtTypeBar.Text;
            string rawMessage = TxtTypeBar.Text;
            string rawClient = clientName.Substring("client:".Length);
            if (!string.IsNullOrEmpty(message))
            {
                SendMessageToServer(message);
                TxtChat.AppendText($"{rawClient}:{rawMessage}\n");
                TxtTypeBar.Clear();
            }
        }

        private async void SendMessageToServer(string message)
        {
            if (stream.CanWrite)
            {
                byte[] data = Encoding.ASCII.GetBytes("chat:" + message);
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush(); 
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
                        TxtChat.AppendText("Disconnected from the server.\n");
                        break;
                    }

                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Dispatcher.Invoke(() => TxtChat.AppendText(message + "\n"));
                }
            }
            catch (Exception ex)
            {
                TxtChat.AppendText($"Error: {ex.Message}\n");
            }
        }

        private void TxtBikeData_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Change state of showing debug messaging
            debugScrolling = !debugScrolling;
        }

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            simulating = !simulating;
            ToggleSimulator();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            clientName = "client:" + TxtName.Text; 
            if (!string.IsNullOrEmpty(clientName))
            {
                byte[] nameData = Encoding.ASCII.GetBytes(clientName);
                stream.Write(nameData, 0, nameData.Length);
                TxtChat.AppendText("Connected as: " + clientName + "\n");

                Task.Run(() => ListenForMessages());
            }
            else
            {
                TxtChat.AppendText("Please enter a valid name before connecting.\n");
            }
        }
    }
}
