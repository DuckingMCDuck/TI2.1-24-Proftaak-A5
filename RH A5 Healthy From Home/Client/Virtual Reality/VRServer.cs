using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client
{
    internal class VRServer
    {
        public static TcpClient vrServer; //TcpClient?
        public static NetworkStream stream; //NetworkStream?
        public static string receivedData;
        public static string hostName = Environment.MachineName;
        public static string sessionId;
        public static string tunnelId;

        public VRServer() { }

        /// <summary>
        /// Starts the VRServer and changes the environment
        /// </summary>
        public static async Task Start()
        {
            await ConnectToVrServerAsync();
        }

        /// <summary>
        /// Connects to the VR Server and initializes Stream
        /// </summary>
        public static async Task ConnectToVrServerAsync()
        {
            try
            {
                // FOR LOCAL RAN SERVER:
                //vrServer = new TcpClient("127.0.0.1", 6666);
                // FOR REMOTE SERVER:
                vrServer = new TcpClient("85.145.62.130", 6666);
            }
            catch (SocketException)
            {
                MainWindow.TextChat.Text = "Error connection to the VRServer!\n";
                return;
            }
            stream = vrServer.GetStream();
            MainWindow.TextChat.Text = "Connected to VR Server!\n";

            // Start listening for packets
            await PacketHandlerAsync();
        }

        /// <summary>
        /// Disconnect from the VR Server (Shutdown). 
        /// This method should not be async, because the connection should be terminated instantly!
        /// </summary>
        public static void ShutdownServer()
        {
            stream.Close();
            vrServer.Close();
            vrServer.Dispose();
            MainWindow.TextChat.Text = "An error has occured, shutting down VRServer...\n";
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

            // Check if sessionId is valid
            if (string.IsNullOrEmpty(sessionId))
            {
                ShutdownServer();
            }

            // Get the tunnelID
            MainWindow.TextBoxBikeData.Text = "Sending session ID packet...";
            SendSessionIdPacket(sessionId);
            string tunnelIdData = await ReceivePacketAsync();
            tunnelId = GetId(tunnelIdData);
            Console.WriteLine($"Received tunnel ID: {tunnelId}");

            // Check if tunnelId is valid
            if (string.IsNullOrEmpty(tunnelId))
            {
                ShutdownServer();
            }
            // Send scene configuration commands
            MainWindow.TextBoxBikeData.Text = "Setting up the environment...";

            // Reset scene:
            await SendTunnelCommand("scene/reset", new
            {

            });

            // SkyBox set time:
            await SendTunnelCommand("scene/skybox/settime", new
            {
                time = 12
            });

            // Create terrain:
            int terrainSize = 128;
            float[,] heights = new float[terrainSize, terrainSize];
            for (int x = 0; x < terrainSize; x++)
                for (int y = 0; y < terrainSize; y++)
                    heights[x, y] = 2 + (float)(Math.Cos(x / 5.0) + Math.Cos(y / 5.0));
            await SendTunnelCommand("scene/terrain/add", new
            {
                size = new[] { terrainSize, terrainSize },
                heights = heights.Cast<float>().ToArray()
            });

            // Create terrain node:
            await SendTunnelCommand("scene/node/add", new
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

            // Create route:
            string routeData = await SendTunnelCommand("route/add", new
            {
                nodes = new[]
                {
                    new 
                    {
                        pos = new[] { 0, 0, 0},
                        dir = new[] { 5, 0, -5}
                    },
                    new
                    {
                        pos = new[] { 50, 0, 0},
                        dir = new[] { 5, 0, 5}
                    },
                    new
                    {
                        pos = new[] { 50, 0, 50},
                        dir = new[] { -5, 0, 5}
                    },
                    new
                    {
                        pos = new[] { 0, 0, 50},
                        dir = new[] { -5, 0, -5}
                    }
                }
            });
            string routeUUID = GetUUID(routeData);

            // Add roads to the route:
            await SendTunnelCommand("scene/road/add", new
            {
                route = routeUUID,
            });

            // Get camera node:
            string getCameraNode = await SendTunnelCommand("scene/node/find", new
            {
                name = "cameraNode"
            });
            Console.WriteLine(getCameraNode);

            // Create bike model node:
            string GuidBikeData = await SendTunnelCommand("scene/node/add", new
            {
                name = "bike",
                components = new
                {
                    transform = new
                    {
                        position = new[] { 0, 0, 0 },
                        scale = 1,
                        rotation = new[] { 0, 0, 0 }
                    },
                    model = new
                    {
                        file = "data/NetworkEngine/models/bike/bike.fbx",
                        cullbackfaces = true
                    }
                }
            });
            string GuidBike = GetUUID(GuidBikeData);

            // Let the bike follow the route:
            await SendTunnelCommand("route/follow", new
            {
                route = routeUUID,
                node = GuidBike,
                speed = 2,
                offset = 0.0,
                rotate = "XYZ",
                smoothing = 1.0,
                followHeight = true,
                rotateOffset = new[] { 0, 0, 0 },
                positionOffset = new[] { 0, 0, 0 }
            });
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
        public static async Task<string> ReceivePacketAsync()
        {
            // Listen for incoming packets from the VR Server
            byte[] prependBuffer = new byte[4];
            int totalBytesRead = 0;

            // Extract prepend data from the buffer
            while (totalBytesRead < 4)
            {
                int bytesRead = await stream.ReadAsync(prependBuffer, totalBytesRead, 4 - totalBytesRead);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Error: Connection closed before reading the full length.");
                    return null; //Error: Return null
                }
                totalBytesRead += bytesRead;
            }

            // Get length of incoming data (decrypt prepend)
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
                    return null; //Error: Return null
                }
                totalBytesRead += bytesRead;
            }

            // Get string of data (decrypt data)
            dataString = Encoding.UTF8.GetString(dataBuffer);
            Console.WriteLine($"Received data [Length {dataLength}]: " + dataString);

            return dataString;
        }

        /// <summary>
        /// Filters out the id from incoming data 
        /// Current id's: session-id, tunnel-id
        /// </summary>
        /// <param name="data"></param>
        public static string GetId(string data)
        {
            // See if we have the host somewhere in the data
            bool hostInData = data.ToLower().Contains(hostName.ToLower());
            if (hostInData)
            {
                string sessionId = "";
                // Split data on clientinfo
                string[] splitted = Regex.Split(data.ToLower(), "clientinfo");
                bool sessionFound = false;
                for (int i = 0; i < splitted.Length; i++)
                {
                    if (splitted[i].Contains(hostName.ToLower()))
                    {
                        // Find the session id of the user with Regex (pattern: 8-4-4-11)
                        string pattern = "([a-z]|[0-9]){8}-([a-z]|[0-9]){4}-([a-z]|[0-9]){4}-([a-z]|[0-9]){4}-([a-z]|[0-9]){12}";

                        // SessionID of current client is in the previous clientinfo data!
                        sessionId = Regex.Match(splitted[i - 1], pattern).Value;
                        sessionFound = true;
                        Console.WriteLine($"Session ID: {sessionId}");
                    }
                }
                if (!sessionFound)
                {
                    Console.WriteLine("Error: Session Id not found!");
                    return null; //Error: Return null
                }
                return sessionId;
            }
            else
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
                if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataObject) &&
                    dataObject.ValueKind == JsonValueKind.Object)
                {
                    JsonNode jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(dataObject);

                    // Check if the incoming object's message is valid
                    string errorMessage = jsonNode.ToString();
                    if (errorMessage.Contains("does not support tunnel"))
                    {
                        Console.WriteLine("Error: JsonObject does not contain an Id!");
                        return null; //Error: Return null
                    }

                    return jsonNode["id"].GetValue<string>();
                }
            }
            return null; //If we even get here -> Error: Return null
        }

        /// <summary>
        /// Initialize starting packet (server response: all current sessions)
        /// </summary>
        public static void SendStartingPacket()
        {
            var alJsonData = new
            {
                id = "session/list",
                data = new
                {

                }
            };

            string jsonPacket = JsonConvert.SerializeObject(alJsonData);
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = BitConverter.GetBytes(data.Length);
            SendPacket(prepend, data);
        }

        /// <summary>
        /// Initialize session packet (server response: tunnel-id)
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

            string jsonPacket = JsonConvert.SerializeObject(alJsonData);
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = BitConverter.GetBytes(data.Length);
            SendPacket(prepend, data);
        }

        /// <summary>
        /// Create specific tunnel commands (server response: variable)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="jsonCommandData"></param>
        public static async Task<string> SendTunnelCommand(string command, object jsonCommandData)
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
            return await ReceivePacketAsync();
        }

        /// <summary>
        /// Retrieves the UUID of a route object specifically
        /// </summary>
        /// <param name="routeData"></param>
        /// <returns></returns>
        private static string GetUUID(string routeData)
        {
            //Set the Json in a tree structure 
            var jsonDocument = JsonDocument.Parse(routeData);
            Console.WriteLine("JsonDoc: " + jsonDocument);

            // Just search for the UUID of the route:
            if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataObject) &&
                dataObject.ValueKind == JsonValueKind.Object)
            {
                JsonNode jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(dataObject);

                // Check if the incoming object's message is valid
                string errorMessage = jsonNode.ToString();
                if (errorMessage.Contains("does not support tunnel"))
                {
                    Console.WriteLine("Error: JsonObject does not contain a UUID!");
                    return null; //Error: Return null
                }

                // Get the data object in the data
                if (jsonNode.AsObject().TryGetPropertyValue("data", out JsonNode dataDataObject))
                {
                    // Get the data object in the data object in the data
                    if (dataDataObject.AsObject().TryGetPropertyValue("data", out JsonNode dataDataDataObject))
                    {
                        // Get the UUID from the data object in the data object in the data
                        return dataDataDataObject["uuid"].GetValue<string>();
                    }
                }
            }
            return null;
        }

    }
}
