using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Client
{
    internal class VRServer
    {
        public static TcpClient vrServer; //TcpClient?
        public static NetworkStream stream; //NetworkStream?
        public static string receivedData;
        public static string hostName = "Laptop-Daan";
        public static string sessionId;
        public static string tunnelId;

        public VRServer() { }

        // TODO: Verschil maken tussen commands die een ID terug sturen (sessionId, tunnelId etc.) en commands waarbij dit niet hoeft?

        /// <summary>
        /// Starts the VRServer and changes the environment
        /// </summary>
        /// <returns></returns>
        public static async Task Start()
        {
            await ConnectToVrServerAsync();
        }

        /// <summary>
        /// Connects to the VR Server and initializes Stream
        /// </summary>
        /// <returns></returns>
        public static async Task ConnectToVrServerAsync()
        {
            // FOR LOCAL RAN SERVER:
            //vrServer = new TcpClient("127.0.0.1", 6666);
            // FOR REMOTE SERVER:
            vrServer = new TcpClient("85.145.62.130", 6666);
            stream = vrServer.GetStream();
            MainWindow.TextChat.Text = "Connected to VR Server!\n";

            // Start listening for packets
            await PacketHandlerAsync();
        }

        /// <summary>
        /// Handles data from incoming packets and sent packets
        /// </summary>
        public static async Task PacketHandlerAsync()
        {
            // Get the sessionID
            MainWindow.TextBoxBikeData.Text = "Sending starting packet...";
            SendStartingPacket();
            string sessionIdData = await ReceivePacketAsync();
            sessionId = GetId(sessionIdData);
            Console.WriteLine($"Received session ID: {sessionId}");

            if (!string.IsNullOrEmpty(sessionId))
            {
                // Get the tunnelID
                MainWindow.TextBoxBikeData.Text = "Sending session ID packet...";
                SendSessionIdPacket(sessionId);
                string tunnelIdData = await ReceivePacketAsync();
                tunnelId = GetId(tunnelIdData);
                Console.WriteLine($"Received tunnel ID: {tunnelId}");

                if (!string.IsNullOrEmpty(tunnelId))
                {
                    // Send scene configuration commands
                    //while (vrServer.Connected)
                    {
                        // Reset scene:
                        //SendTunnelCommand("scene/reset", "{}");

                        // SkyBox time & update (dynamic works, static doesn't):
                        //int terrainSize = 5;
                        float[,] heights = new float[32, 32];
                        for (int x = 0; x < 32; x++)
                            for (int y = 0; y < 32; y++)
                                heights[x, y] = 2 + (float)(Math.Cos(x / 5.0) + Math.Cos(y / 5.0));

                        //int[] heights = new int[terrainSize*terrainSize];
                        //for (int i = 0; i < heights.Length; i++)
                        //{
                        //    heights[i] = 50;
                        //}

                        SendTunnelCommand("scene/skybox/settime", new
                        {
                            time = 24
                        });
                        //SendTunnelCommand($"scene/skybox/settime","{ time :24 }", null);
                        //SendTunnelCommand("scene/terrain/add", " ", new
                        //{
                        //    size = new[] { 32, 32 },
                        //    heights = heights.Cast<float>().ToArray()
                        //});
                        SendTunnelCommand("scene/terrain/add", new
                        {
                            size = new[] { 32, 32 },
                            heights = heights.Cast<float>().ToArray()
                        });
                        SendTunnelCommand("scene/node/add", new
                        {
                            name = "floor",
                            components = new
                            {
                                transform = new
                                {
                                    position = new[] { -16, 0, -16 },
                                    scale = 1
                                },
                                terrain = new
                                {

                                }
                            }
                        });
                        //SendTunnelCommand("scene/node/add", "", new
                        //{
                        //    name = "floor",
                        //    components = new
                        //    {
                        //        transform = new
                        //        {
                        //            position = new[] { -16, 0, -16 },
                        //            scale = 1
                        //        },
                        //        terrain = new
                        //        {

                        //        }
                        //    }
                        //});
                        //"{ size : [256, 256], heights : []}"


                        // Create terrain/ groundplane (W.I.P.):
                        //int[] heights = new int[65536];
                        //Random rnd = new Random();
                        //for (int i = 0; i < heights.Length; i++)
                        //{
                        //    heights[i] = rnd.Next(0, 50);
                        //}
                        //SendTunnelCommand(tunnelId, "scene/terrain/add", "{\"size\" : [ 256, 256 ],\"heights\" : heights}");

                        // Create route (bug):
                        //int[,] routePoints = { { 0, 0, 0 }, { 5, 0, -5 }, { 5, 0, 5 }, { -5, 0, 5 }, { -5, 0, -5 } };
                        //SendTunnelCommand(tunnelId, "route/add", "{\"nodes\" : [{\"pos\" : [ 0, 0, 0  ],\"dir\" : [ 5, 0, -5]},{\"pos\" : [ 50, 0, 0 ],\"dir\" : [ 5, 0, 5]},{\"pos\" : [ 50, 0, 50],\"dir\" : [ -5, 0, 5]},{\"pos\" : [ 0, 0, 50 ],\"dir\" : [ -5, 0, -5]},]}");
                        //string routeId = await ReceivePacketAsync();
                        //Console.WriteLine($"Received route ID: {routeId}");

                        // Add roads to route (needs route first):
                        //SendTunnelCommand(tunnelId, "scene/road/add", "{route : routeId}");
                    }
                }
            }
        }

        /// <summary>
        /// Sends the packet to the VR Server
        /// </summary>
        /// <param name="prepend"></param>
        /// <param name="data"></param>
        public static void SendPacket(byte[] prepend, byte[] data)
        {
            // Add the prepend and data together and send this packet
            byte[] combinedArray = new byte[prepend.Length + data.Length];
            Array.Copy(prepend, 0, combinedArray, 0, prepend.Length);
            Array.Copy(data, 0, combinedArray, prepend.Length, data.Length);
            Console.WriteLine(Encoding.ASCII.GetString(combinedArray));
            stream.Write(combinedArray, 0, combinedArray.Length);
        }

        /// <summary>
        /// Listens for incoming packets from the VR Server
        /// </summary>
        /// <returns></returns>
        public static async Task<string> ReceivePacketAsync()
        {
            // Listen for incoming packets
            byte[] prependBuffer = new byte[4];
            int totalBytesRead = 0;

            // Extract prepend data from the buffer
            while (totalBytesRead < 4)
            {
                int bytesRead = await stream.ReadAsync(prependBuffer, totalBytesRead, 4 - totalBytesRead);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Error: Connection closed before reading the full length.");
                    return null;
                }
                totalBytesRead += bytesRead;
            }
            // Get length of incoming data
            int dataLength = BitConverter.ToInt32(prependBuffer, 0);
            Console.WriteLine("datalenght: " + dataLength);
            totalBytesRead = 0;

            // Extract other data from the buffer
            string dataString = "";
            byte[] dataBuffer = new byte[dataLength];
            while (totalBytesRead < dataLength)
            {
                int bytesRead = await stream.ReadAsync(dataBuffer, totalBytesRead, dataLength - totalBytesRead);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Error: Connection closed before reading the full packet.");
                    return null;
                }
                totalBytesRead += bytesRead;
            }
            // Get string of data
            dataString = Encoding.UTF8.GetString(dataBuffer);
            Console.WriteLine($"Received data [Length {dataLength}]: " + dataString);

            return dataString;
        }

        /// <summary>
        /// Gets the id from incoming data (sessionId, tunnelId etc.)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetId(string data)
        {
            
            // See if we have the host somewhere in the data
            bool hostInData = data.ToLower().Contains(hostName.ToLower());
            if (hostInData)
            {
                string sessionId = "";
                // Split data on '{' character
                string[] splitted = Regex.Split(data.ToLower(), "clientinfo");
                bool sessionFound = false;
                for (int i = 0; i < splitted.Length; i++)
                {
                    //To print the first ID (which is wrong!):
                    //string a = splitted[2].Trim();
                    if (splitted[i].Contains(hostName.ToLower()))
                    {
                        // Find the session id of the user with Regex (pattern 8-4-4-11)
                        string pattern = "([a-z]|[0-9]){8}-([a-z]|[0-9]){4}-([a-z]|[0-9]){4}-([a-z]|[0-9]){4}-([a-z]|[0-9]){12}";
                        sessionId = Regex.Match(splitted[i - 1], pattern).Value;
                        sessionFound = true;
                        Console.WriteLine($"Session ID: {sessionId}");
                    }
                }
                if (!sessionFound)
                {
                    Console.WriteLine("Error: Session Id not found!");
                }
                return sessionId;
            } else
            {
                //Set the Json in a tree structure 
                var jsonDocument = JsonDocument.Parse(data);
                Console.WriteLine("JsonDoc: " + jsonDocument);

                // Just search for the Id, which can be inside of an array or object, so we have 2 cases:
                if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Array &&
                    dataElement.GetArrayLength() > 0)
                {
                    return dataElement[0].GetProperty("id").GetString();
                }
                try
                {
                    if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataObject) &&
                        dataObject.ValueKind == JsonValueKind.Object)
                    {
                        JsonNode jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(dataObject);
                        return jsonNode["id"].GetValue<string>();
                    }
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine("No Value Returned, So VR might be inactive");
                }
            }
            return null;
        }

        /// <summary>
        /// Initialize starting packet
        /// </summary>
        public static void SendStartingPacket()
        {
            //string jsonPacket = "{\"id\" : \"session/list\"}";
            var alJsonData = new
            {
                id = "session/list",
                data = new
                {
                    
                }
            };
            string jsonPacket = JsonConvert.SerializeObject(alJsonData);
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            //byte[] prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            byte[] prepend = BitConverter.GetBytes(data.Length);

            SendPacket(prepend, data);
        }

        /// <summary>
        /// Initialize session packet
        /// </summary>
        /// <param name="sessionId"></param>
        public static void SendSessionIdPacket(string sessionId)
        {
            var alJsonData = new
            {
                id = "tunnel/create",
                data = new
                {
                    session = sessionId,
                    key = ""
                }
            };
            //string jsonPacket = $"{{\"id\" : \"tunnel/create\",\"data\" : {{\"session\" : \"{sessionId}\",\"key\" : \"\"}}}}";
            string jsonPacket = JsonConvert.SerializeObject(alJsonData);
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = BitConverter.GetBytes(data.Length);

            //byte[] prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }

        /// <summary>
        /// Initialize a tunnel command (Generic command for all tunnel functions)
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="command"></param>
        /// <param name="commandData"></param>
        //public static void SendTunnelCommand(string command, string commandData, Object jsonCommandData)
        //{
        //    byte[] data;
        //    if (jsonCommandData == null)
        //    {
        //        string jsonPacket = $"{{\"id\" : \"tunnel/send\",\"data\" :{{\"dest\" : \"{tunnelId}\",\"data\" : {{\"id\" : \"{command}\",\"data\" : {commandData}}}}}}}";
        //        data = Encoding.ASCII.GetBytes(jsonPacket);
        //    }
        //    else
        //    {
        //        //string jsonPacket = $"{{\"id\" : \"tunnel/send\",\"data\" :{{\"dest\" : \"{tunnelId}\",\"data\" : {jsonCommandData}}}}}}}";
        //        //TODO probeer in je code vooral met (anonieme) objecten te werken, en dan pas helemaal aan het einde als je gaat sturen serializen, want nu ga je jsonstrings in strings plakken, dat wil je niet 

        //        string jsonCommand = JsonConvert.SerializeObject(jsonCommandData);
        //        string totalString = $"{{\"id\" : \"tunnel/send\",\"data\" :{{\"dest\" : \"{tunnelId}\",\"data\" : {{\"id\" : \"{command}\",\"data\" : {jsonCommand}}}}}}}";
        //        data = Encoding.ASCII.GetBytes(totalString);
        //    }

        //    byte[] prepend = BitConverter.GetBytes(data.Length);
        //    SendPacket(prepend, data);
        //}

        public static void SendTunnelCommand(string command, object jsonCommandData)
        {

            var alJsonData = new
            {
                id = "tunnel/send",
                data = new
                {
                    dest = tunnelId,
                    data = new
                    {
                        id = command,
                        data = jsonCommandData
                    }
                }
            };
            string jsonPacket = JsonConvert.SerializeObject(alJsonData);

            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = BitConverter.GetBytes(data.Length);
            SendPacket(prepend, data);
        }

      
    }
}
