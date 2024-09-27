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
        internal static MainWindow Client; // Main property to store the main window instance for accessing it from other classes

        // Properties to get text from another class
        internal string DebugText {
            get { return TextBoxBikeData.Text.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { TextBoxBikeData.AppendText(value); })); } 
        }

        // Publics:
        public static TcpClient TcpClientConnection = new TcpClient();
        public static List<Tuple<string, byte[]>> SessionData = new List<Tuple<string, byte[]>>();
        public static Simulator Simulator = new Simulator();

        // Toolbox-Items:
        public static TextBox TextBoxBikeData;

        // Privates:
        private static bool SessionRunning = false;
        private static bool DebugScrolling = true;
        private static bool Simulating = false;

        public MainWindow()
        {
            InitializeComponent();
            Client = this; // Innitialize class 'Client' property
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to the server
            TcpClientConnection.Connect("localhost", 15243);

            // Create public variables for items in the Toolbox
            TextBoxBikeData = TxtBikeData;

            #region Connecting via bike (FietsDemo code)
            //UsingBicycle();
            #endregion

            #region Connecting via Simulator
            // Start Simulator on a new Thread
            Simulating = true;
            ToggleSimulator();
            #endregion
        }

        // Self-written code:
        private static async void UsingBicycle()
        {
            int ErrorCode = 0;
            BLE BleBike = new BLE();
            BLE BleHeart = new BLE();
            Thread.Sleep(1000); // We need some time to list available devices

            // List available devices
            List<String> BleBikeList = BleBike.ListDevices();
            TextBoxBikeData.AppendText("Devices found: ");
            foreach (var Name in BleBikeList)
            {
                TextBoxBikeData.AppendText($"Device: {Name}");
            }

            // Connect to bike using the last 5 digits of the serial number:
            ErrorCode = await BleBike.OpenDevice("Tacx Flux 00438");
            // __TODO__ Error check

            // Print availible services
            var Services = BleBike.GetServices;
            foreach (var Service in Services)
            {
                TextBoxBikeData.AppendText($"Service: {Service}");
            }

            // Set service
            ErrorCode = await BleBike.SetService("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
            // __TODO__ error check

            // Subscribe
            BleBike.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
            ErrorCode = await BleBike.SubscribeToCharacteristic("6e40fec2-b5a3-f393-e0a9-e50e24dcca9e");
            // __TODO__ error check

            // Heart rate
            ErrorCode = await BleHeart.OpenDevice("Decathlon Dual HR");
            // __TODO__ error check

            await BleHeart.SetService("HeartRate");

            BleHeart.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
            await BleHeart.SubscribeToCharacteristic("HeartRateMeasurement");
        }

        private static void UsingSimulator()
        {
            // Simulating bike data
            while (Simulating)
            {
                Simulator.SimulateData();
            }
        }

        private static void BleBike_SubscriptionValueChanged(object sender, BLESubscriptionValueChangedEventArgs e)
        {
            if (SessionRunning == true)
            {
                // Print incoming data from bike
                TextBoxBikeData.AppendText($"Received from {e.ServiceName}: {BitConverter.ToString(e.Data).Replace("-", " ")}, {Encoding.UTF8.GetString(e.Data)}");

                // Add data to session
                SessionData.Add(new Tuple<string, byte[]>(e.ServiceName, e.Data));
            }
        }

        private static void ToggleSimulator()
        {
            if (Simulating)
            {
                Thread NewThread = new Thread(() => {
                    UsingSimulator();
                });
                // Threads running in the background close if the application closes
                NewThread.IsBackground = true;
                NewThread.Start();
            }
        }

        // Generated Events:
        private void TxtBikeData_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Show Bike/ Simulator data (Debug)
            if (DebugScrolling)
                TxtBikeData.ScrollToEnd();
            // Keep only the recent data availible to scroll through
            if (TxtBikeData.LineCount > 300)
                TxtBikeData.Text = TxtBikeData.Text.Substring(TxtBikeData.LineCount - 100);
        }

        private void ReadChat_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Always scroll to end of chat
            ReadChat.ScrollToEnd();
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            // Get written message and send it to the chat (server)
            string Message = TypeBar.Text;
            if (!string.IsNullOrEmpty(Message))
            {
                ReadChat.AppendText(Message + "\n");// Exchange this for sending message logic
                TypeBar.Clear();
            }
        }

        private void TxtBikeData_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Change state of showing debug messaging
            DebugScrolling = !DebugScrolling;
        }

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            Simulating = !Simulating;
            ToggleSimulator();
        }


        // EXAMPLE CODE:

        //// LET OP: username moet gelijk zijn aan password om correct te werken=
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
