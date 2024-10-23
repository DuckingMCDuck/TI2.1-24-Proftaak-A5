using Client.Virtual_Reality;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

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
            // Send scene configuration commands
            MainWindow.TextBoxBikeData.Text = "Setting up the environment...";

            // Get the sessionID
            string sessionIdData = await SendStartingPacket();
            sessionId = GetId(sessionIdData);
            Console.WriteLine($"Received session ID: {sessionId}");

            // Check if sessionId is valid
            if (string.IsNullOrEmpty(sessionId))
            {
                ShutdownServer();
            }

            // Get the tunnelID
            string tunnelIdData = await SendSessionIdPacket(sessionId);
            tunnelId = GetId(tunnelIdData);
            Console.WriteLine($"Received tunnel ID: {tunnelId}");

            // Check if tunnelId is valid
            if (string.IsNullOrEmpty(tunnelId))
            {
                ShutdownServer();
            }
           
            // OPTIONAL:
            // Reset scene:
            await SendTunnelCommand("scene/reset", JsonBuilder.EmptyObjectData());
            // Get scene data:
            string getSceneData = await SendTunnelCommand("scene/get", null);
            Console.WriteLine("ALL SCENE DATA: " + getSceneData);

            // REQUIRED:
            // SkyBox set time:
            await SendTunnelCommand("scene/skybox/settime", JsonBuilder.GetSkyBoxTimeData(12));

            // Create terrain:
            int terrainSize = 128;
            await SendTunnelCommand("scene/terrain/add", JsonBuilder.GetTerrainData(terrainSize));

            // Create terrain node:
            await SendTunnelCommand("scene/node/add", JsonBuilder.CreateComponentData("floor",new int[] {-16, 0, -16}));


            // Create route (F1 Monza Circuit):
            string routeData = await SendTunnelCommand("route/add", JsonBuilder.GetRouteData());
            string routeUuid = GetUuid(routeData);

            // Add roads to the route:
            await SendTunnelCommand("scene/road/add", JsonBuilder.AddRoadsData(routeUuid));

            // Find camera node:
            string getCameraNodeData = await SendTunnelCommand("scene/node/find", JsonBuilder.FindNodeData("Camera"));
            if (getCameraNodeData != null)
            {
                // Get camera node
                string cameraNodeId = GetUuid(getCameraNodeData);
                Console.WriteLine("Camera Node ID: " + cameraNodeId);
                // Let the camera follow the route:
                await SendTunnelCommand("route/follow", JsonBuilder.LetItemFollowRouteData(routeUuid, cameraNodeId, "XYZ", 2));
            }

            // Find groundplane node:
            string getGroundplaneNodeData = await SendTunnelCommand("scene/node/find", JsonBuilder.FindNodeData("GroundPlane"));
            if (getGroundplaneNodeData != null)
            {
                // Get camera node
                string groundPlaneId = GetUuid(getGroundplaneNodeData);
                Console.WriteLine("Groundplane ID: " + groundPlaneId);
                await SendTunnelCommand("scene/node/delete", JsonBuilder.DeleteNodeData(groundPlaneId));
            }

            // Create bike model node:
            string GuidBikeData = await SendTunnelCommand("scene/node/add", JsonBuilder.CreateModelData("data/NetworkEngine/models/bike/bike.fbx", "bike", new int[] { 0,0,0}, new int[] { 0,0,0}));
            string GuidBike = GetUuid(GuidBikeData);
            if (GuidBike != null)
            {
                // Let the bike follow the route:
                await SendTunnelCommand("route/follow", JsonBuilder.LetItemFollowRouteData(routeUuid, GuidBike, "XYZ", 2));
            };

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
        public static async Task<string> SendStartingPacket()
        {
            // Create session list command
            var alJsonData = JsonBuilder.GetStartingPacketData();

            string jsonPacket = JsonConvert.SerializeObject(alJsonData);
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = BitConverter.GetBytes(data.Length);
            SendPacket(prepend, data);
            return await ReceivePacketAsync();
        }

        /// <summary>
        /// Initialize session packet (server response: tunnel-id)
        /// </summary>
        /// <param name="sessionId"></param>
        public static async Task<string> SendSessionIdPacket(string sessionId)
        {
            // Create tunnel create command
            var alJsonData = JsonBuilder.GetSessionIdPacketData(sessionId);

            string jsonPacket = JsonConvert.SerializeObject(alJsonData);
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = BitConverter.GetBytes(data.Length);
            SendPacket(prepend, data);
            return await ReceivePacketAsync();
        }

        /// <summary>
        /// Create specific tunnel commands (server response: variable)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="jsonCommandData"></param>
        public static async Task<string> SendTunnelCommand(string command, object jsonCommandData)
        {
            // Create tunnel send command
            var alJsonData = JsonBuilder.GetTunnelCommandData(tunnelId, command, jsonCommandData);

            string jsonPacket = JsonConvert.SerializeObject(alJsonData);
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = BitConverter.GetBytes(data.Length);
            SendPacket(prepend, data);
            return await ReceivePacketAsync();
        }

        /// <summary>
        /// Retrieves the UUID of a route object specifically
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string GetUuid(string data)
        {
            //Set the Json in a tree structure 
            var jsonDocument = JsonDocument.Parse(data);
            Console.WriteLine("JsonDoc: " + jsonDocument);

            // Get the data object in the Json Document
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

                // Get the data object in the data object in the Json Document
                if (dataObject.TryGetProperty("data", out JsonElement dataDataObject) &&
                    dataDataObject.ValueKind == JsonValueKind.Object)
                {
                    // Check if we are searching in the recieved data of a command or the data of the entire scene (2 cases):

                    // Get the data object in the data object in the data object in the Json Document
                    if (dataDataObject.TryGetProperty("data", out JsonElement dataDataDataObject) &&
                        dataDataDataObject.ValueKind == JsonValueKind.Object)
                    {
                        // Get the UUID property from the data object in the data object in the data object in the Json Document
                        return dataDataDataObject.GetProperty("uuid").GetString();
                    }

                    // Get the data array in the data object in the data object in the Json Document (scene data search)
                    else if (dataDataObject.TryGetProperty("data", out JsonElement dataDataDataElement) &&
                        dataDataDataElement.ValueKind == JsonValueKind.Array &&
                        dataDataDataElement.GetArrayLength() > 0)
                    {
                        return dataDataDataElement[0].GetProperty("uuid").GetString();
                    }
                }
            }
            return null;
        }

    }
}
