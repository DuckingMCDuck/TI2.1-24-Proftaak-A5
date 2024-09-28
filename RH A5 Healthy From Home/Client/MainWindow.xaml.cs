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

        // Properties to update TextBoxes (From another class)
        internal string debugText {
            get { return TextBoxBikeData.Text.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { TextBoxBikeData.AppendText(value); })); } 
        }
        internal string chatText
        {
            get { return TextChat.Text.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { TextChat.AppendText(value); })); }
        }

        // Publics:
        public static TcpClient tcpClient = new TcpClient();
        public static List<Tuple<string, byte[]>> sessionData = new List<Tuple<string, byte[]>>();
        public static Simulator simulator = new Simulator();
        //private static VRServer vRServer = new VRServer();

        // Privates:
        private static bool sessionRunning = false;
        private static bool debugScrolling = true;
        private static bool simulating = true;

        // Toolbox-Items:
        public static TextBox TextBoxBikeData;
        public static TextBox TextChat;

        public MainWindow()
        {
            InitializeComponent();
            client = this; // Initialize class 'Client' property
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to the server
            tcpClient.Connect("localhost", 15243);

            // Create public variables for items in the Toolbox (Schrijf volledig uit: Txt -> TextBox, Btn -> Button etc.)
            TextBoxBikeData = TxtBikeData;
            TextChat = TxtChat;

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
            simulating = true;
            ToggleSimulator();
        }

        private static void ToggleSimulator()
        {
            TextChat.AppendText("Simulator turned " + (simulating ? "ON" : "OFF") + "!\n");
            if (simulating)
            {
                Thread simulatorThread = new Thread(() => {
                    StartSimulator();
                });
                // Threads running in the background close if the application closes
                simulatorThread.IsBackground = true;
                simulatorThread.Start();
            }
        }

        private static void StartSimulator()
        {
            // Simulating bike data
            while (simulating)
            {
                simulator.SimulateData();
                // Send DebugText to simulator
                SendToSimulator(client.debugText);
            }
        }

        private static void SendToSimulator(string debugText)
        {
            // Send debugText to simulator via TCP/IP
            using (NetworkStream stream = tcpClient.GetStream())
            {
                byte[] buffer = new byte[1024];
                buffer = Encoding.UTF8.GetBytes(debugText);
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        private static void BleBike_SubscriptionValueChanged(object sender, BLESubscriptionValueChangedEventArgs e)
        {
            if (sessionRunning == true)
            {
                // Print incoming data from bike
                TextBoxBikeData.AppendText($"Received from {e.ServiceName}: {BitConverter.ToString(e.Data).Replace("-", " ")}, {Encoding.UTF8.GetString(e.Data)}");

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
            string Message = TxtTypeBar.Text;
            if (!string.IsNullOrEmpty(Message))
            {
                TxtChat.AppendText(Message + "\n");// Exchange this for sending message logic
                TxtTypeBar.Clear();
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


        // EXAMPLE CODE:

        //// LET OP: username moet gelijk zijn aan password om correct te werken
        //private static string password;
        //private static TcpClient client;
        //private static NetworkStream stream;
        //private static byte[] buffer = new byte[1024];
        //private static string totalBuffer;
        //private static string username;

        //private static bool loggedIn = false;

        //static void Main(string[] args)
        //{
        //    Console.WriteLine("Hello Client!");
        //    Console.WriteLine("Wat is je gebruikersnaam? ");
        //    username = Console.ReadLine();
        //    Console.WriteLine("Wat is je wachtwoord? ");
        //    password = Console.ReadLine();

        //    client = new TcpClient();
        //    client.BeginConnect("localhost", 15243, new AsyncCallback(OnConnect), null);

        //    while (true)
        //    {
        //        Console.WriteLine("Voer een chatbericht in:");
        //        string newChatMessage = Console.ReadLine();
        //        if (loggedIn)
        //            write($"chat\r\n{newChatMessage}");
        //        else
        //            Console.WriteLine("Je bent nog niet ingelogd");
        //    }
        //}

        //private static void OnConnect(IAsyncResult ar)
        //{
        //    client.EndConnect(ar);
        //    Console.WriteLine("Verbonden!");
        //    stream = client.GetStream();
        //    stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnRead), null);
        //    write($"login\r\n{username}\r\n{password}");
        //}

        //private static void OnRead(IAsyncResult ar)
        //{
        //    int receivedBytes = stream.EndRead(ar);
        //    string receivedText = System.Text.Encoding.ASCII.GetString(buffer, 0, receivedBytes);
        //    totalBuffer += receivedText;

        //    while (totalBuffer.Contains("\r\n\r\n"))
        //    {
        //        string packet = totalBuffer.Substring(0, totalBuffer.IndexOf("\r\n\r\n"));
        //        totalBuffer = totalBuffer.Substring(totalBuffer.IndexOf("\r\n\r\n") + 4);
        //        string[] packetData = Regex.Split(packet, "\r\n");
        //        handleData(packetData);
        //    }
        //    stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnRead), null);
        //}
        //private static void write(string data)
        //{
        //    var dataAsBytes = System.Text.Encoding.ASCII.GetBytes(data + "\r\n\r\n");
        //    stream.Write(dataAsBytes, 0, dataAsBytes.Length);
        //    stream.Flush();
        //}

        //private static void handleData(string[] packetData)
        //{
        //    Console.WriteLine($"Packet ontvangen: {packetData[0]}");

        //    switch (packetData[0])
        //    {
        //        case "login":
        //            if (packetData[1] == "ok")
        //            {
        //                Console.WriteLine("Logged in!");
        //                loggedIn = true;
        //            }
        //            else
        //                Console.WriteLine(packetData[1]);
        //            break;
        //        case "chat":
        //            Console.WriteLine($"Chat ontvangen: '{packetData[1]}'");
        //            break;
        //    }

        //}
    }
}
