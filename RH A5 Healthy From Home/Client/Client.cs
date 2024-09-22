using Avans.TI.BLE;
using Simulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class Client
    {
        //TODO: implement sending data to server (real-time/sessions?), add BLE library
        public static List<Tuple<string, byte[]>> sessionData = new List<Tuple<string, byte[]>>();
        private static bool sessionRunning = false;
        static async Task Main(string[] args)
        {
            //#region Connecting via bike (FietsDemo code)
            //int errorCode = 0;
            //BLE bleBike = new BLE();
            //BLE bleHeart = new BLE();
            //Thread.Sleep(1000); // We need some time to list available devices

            //// List available devices
            //List<String> bleBikeList = bleBike.ListDevices();
            //Console.WriteLine("Devices found: ");
            //foreach (var name in bleBikeList)
            //{
            //    Console.WriteLine($"Device: {name}");
            //}

            //// Connect to bike using the last 5 digits of the serial number:
            //errorCode = await bleBike.OpenDevice("Tacx Flux 00438");
            //// __TODO__ Error check

            //// Print availible services
            //var services = bleBike.GetServices;
            //foreach (var service in services)
            //{
            //    Console.WriteLine($"Service: {service}");
            //}

            //// Set service
            //errorCode = await bleBike.SetService("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
            //// __TODO__ error check

            //// Subscribe
            //bleBike.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
            //errorCode = await bleBike.SubscribeToCharacteristic("6e40fec2-b5a3-f393-e0a9-e50e24dcca9e");
            //// __TODO__ error check

            //// Heart rate
            //errorCode = await bleHeart.OpenDevice("Decathlon Dual HR");
            //// __TODO__ error check

            //await bleHeart.SetService("HeartRate");

            //bleHeart.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
            //await bleHeart.SubscribeToCharacteristic("HeartRateMeasurement");
            //#endregion


            #region Connecting via Simulator
            // Simulating bike data
            Simulator.Simulator simulator = new Simulator.Simulator();
            simulator.StartSimulation();
            #endregion

            Console.Read();
        }

        private static void BleBike_SubscriptionValueChanged(object sender, BLESubscriptionValueChangedEventArgs e)
        {
            if (sessionRunning == true)
            {
                // Print incoming data from bike
                Console.WriteLine("Received from {0}: {1}, {2}", e.ServiceName,
                    BitConverter.ToString(e.Data).Replace("-", " "),
                    Encoding.UTF8.GetString(e.Data));

                // Add data to session
                sessionData.Add(new Tuple<string, byte[]>(e.ServiceName, e.Data));
            }
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
