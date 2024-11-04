using Client.Virtual_Reality;
using HealthyFromHomeApp.Clients;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Client.Virtual_Reality
{
    internal class VRServer
    {
        private static ClientMainWindow clientInstance; // To access GUI elements

        // General objects:
        public static TcpClient vrServer;
        public static NetworkStream stream;
        public static string receivedData;
        public static string hostName = Environment.MachineName;
        public static bool isConnected = false;

        // Specific (string) objects:
        public static string sessionId;
        public static string tunnelId;
        public static string routeUuid;
        public static string panelId;
        public static string guidBike;
        public static string cameraNodeId;

        public VRServer(ClientMainWindow client) { clientInstance = client; }

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
                clientInstance.chatText += "Error connection to the VRServer!\n";
                return;
            }
            stream = vrServer.GetStream();
            clientInstance.chatText += "Connected to VR Server!\n";
            isConnected = true;

            // Start listening for packets
            await PacketHandlerAsync();
        }

        /// <summary>
        /// Disconnect from the VR Server (Shutdown). 
        /// This method should not be async, because the connection should be terminated instantly!
        /// </summary>
        public static async void ShutdownServer()
        {
            clientInstance.chatText += "An error has occured, shutting down VRServer...\n";
            stream.Close();
            vrServer.Close();
            vrServer.Dispose();

            // Automatically restart the VRServer after 10 seconds (pause this Thread)
            await Task.Delay(10000);
            clientInstance.chatText += "Trying to restart VRServer...\n";
            await Start();
        }

        /// <summary>
        /// Handles data from incoming packets and sent packets
        /// </summary>
        public static async Task PacketHandlerAsync()
        {
            // Send scene configuration commands
            clientInstance.chatText += "Setting up the environment...";

            // Get the sessionID
            string sessionIdData = await SendStartingPacket();
            sessionId = GetId(sessionIdData);
            Debug.WriteLine($"Received session ID: {sessionId}");

            // Check if sessionId is valid
            if (string.IsNullOrEmpty(sessionId))
            {
                ShutdownServer();
                return;
            }

            // Get the tunnelID
            string tunnelIdData = await SendSessionIdPacket(sessionId);
            tunnelId = GetId(tunnelIdData);
            Debug.WriteLine($"Received tunnel ID: {tunnelId}");

            // Check if tunnelId is valid
            if (string.IsNullOrEmpty(tunnelId))
            {
                ShutdownServer();
                return;
            }

            // Always Reset the scene:
            await SendTunnelCommand("scene/reset", JsonBuilder.EmptyObjectData());
            // Get scene data:
            string getSceneData = await SendTunnelCommand("scene/get", null);
            Debug.WriteLine("ALL SCENE DATA: " + getSceneData);

            // SkyBox set time:
            await SendTunnelCommand("scene/skybox/settime", JsonBuilder.GetSkyBoxTimeData(8));

            // Find groundplane node:
            string getGroundplaneNodeData = await SendTunnelCommand("scene/node/find", JsonBuilder.FindNodeData("GroundPlane"));
            if (getGroundplaneNodeData != null)
            {
                // Get groundplane ID & delete groundplane node:
                string groundPlaneId = GetUuid(getGroundplaneNodeData);
                Debug.WriteLine("Groundplane ID: " + groundPlaneId);
                await SendTunnelCommand("scene/node/delete", JsonBuilder.DeleteNodeData(groundPlaneId));
            }

            // Create terrain:
            int terrainSize = 128;
            await SendTunnelCommand("scene/terrain/add", JsonBuilder.GetTerrainData(terrainSize));

            // Create terrain node:
            await SendTunnelCommand("scene/node/add", JsonBuilder.CreateTerrainData("floorterrain", new int[] { -16, 0, -16 }));

            // Create route (F1 Monza Circuit) & get route UUID:
            string routeData = await SendTunnelCommand("route/add", JsonBuilder.GetRouteData());
            routeUuid = GetUuid(routeData);

            // Add roads to the route:
            await SendTunnelCommand("scene/road/add", JsonBuilder.AddRoadsData(routeUuid));

            // Find camera node:
            string getCameraNodeData = await SendTunnelCommand("scene/node/find", JsonBuilder.FindNodeData("Camera"));
            // Get camera node UUID:
            cameraNodeId = GetUuid(getCameraNodeData);
            Debug.WriteLine("Camera Node ID: " + cameraNodeId);

            if (cameraNodeId != null)
            {
                // Create bike model node & get bike model UUID:
                string guidBikeData = await SendTunnelCommand("scene/node/add", JsonBuilder.CreateModelData("data/NetworkEngine/models/bike/bike.fbx", "bike", cameraNodeId, new int[] { 0, 0, 0 }, new int[] { 0, 270, 0 }));
                guidBike = GetUuid(guidBikeData);

                if (guidBike != null)
                {
                    // Create panel node & get panel UUID:
                    string panelData = await SendTunnelCommand("scene/node/add", JsonBuilder.CreatePanelData("panel", guidBike, new int[] { -2, 2, -2 }, new int[] { 0, 45, 0 }));
                    panelId = GetUuid(panelData);

                    if (panelId != null)
                    {
                        // Set panel color & clear before usage:
                        await SendTunnelCommand("scene/panel/setclearcolor", JsonBuilder.SetPanelColorData(panelId, new int[] { 1, 1, 1, 1 }));
                        await ClearPanel();

                        // Let the Camera (-> parent of Bike -? parent of Panel) follow the route:
                        await SendTunnelCommand("route/follow", JsonBuilder.LetItemFollowRouteData(routeUuid, cameraNodeId, "XYZ", 0, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }));
                    }
                    else
                    {
                        ShutdownServer();
                        return;
                    }
                }
                else
                {
                    ShutdownServer();
                    return;
                }
            }
            else
            {
                ShutdownServer();
                return;
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
                    Debug.WriteLine("Error: Connection closed before reading the full length.");
                    return null; //Error: Return null
                }
                totalBytesRead += bytesRead;
            }

            // Get length of incoming data (decrypt prepend)
            int dataLength = BitConverter.ToInt32(prependBuffer, 0);
            Debug.WriteLine("datalenght: " + dataLength);
            totalBytesRead = 0;

            // Extract other data from the buffer
            string dataString = "";
            byte[] dataBuffer = new byte[dataLength];
            while (totalBytesRead < dataLength)
            {
                int bytesRead = await stream.ReadAsync(dataBuffer, totalBytesRead, dataLength - totalBytesRead);
                if (bytesRead == 0)
                {
                    Debug.WriteLine("Error: Connection closed before reading the full packet.");
                    return null; //Error: Return null
                }
                totalBytesRead += bytesRead;
            }

            // Get string of data (decrypt data)
            dataString = Encoding.UTF8.GetString(dataBuffer);
            Debug.WriteLine($"Received data [Length {dataLength}]: " + dataString);

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
                        Debug.WriteLine($"Session ID: {sessionId}");
                    }
                }
                if (!sessionFound)
                {
                    Debug.WriteLine("Error: Session Id not found!");
                    return null; //Error: Return null
                }
                return sessionId;
            }
            else
            {
                //Set the Json in a tree structure 
                var jsonDocument = JsonDocument.Parse(data);

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
                        Debug.WriteLine("Error: JsonObject does not contain an Id!");
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

            // Get the data object in the Json Document
            if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataObject) &&
                dataObject.ValueKind == JsonValueKind.Object)
            {
                JsonNode jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(dataObject);

                // Check if the incoming object's message is valid
                string errorMessage = jsonNode.ToString();
                if (errorMessage.Contains("does not support tunnel"))
                {
                    Debug.WriteLine("Error: JsonObject does not contain a UUID!");
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
                        if (dataDataDataObject.ValueKind.ToString() == "Undefined") // Undefined
                            return null; // Error: Return null
                        if (dataDataDataObject.ValueKind == JsonValueKind.Object &&
                            dataDataDataObject.TryGetProperty("button", out JsonElement wrongElement)) // Trigger buttons
                            return null; // Error: Return null

                        // Get the UUID property from the data object in the data object in the data object in the Json Document
                        return dataDataDataObject.GetProperty("uuid").GetString();
                    }

                    // Get the data array in the data object in the data object in the Json Document (scene data search)
                    else if (dataDataObject.TryGetProperty("data", out JsonElement dataDataDataElement) &&
                        dataDataDataElement.ValueKind == JsonValueKind.Array &&
                        dataDataDataElement.GetArrayLength() > 0)
                    {
                        // Get the UUID property from the data array in the data object in the data object in the Json Document
                        return dataDataDataElement[0].GetProperty("uuid").GetString();
                    }
                }
            }
            return null; //If we even get here -> Error: Return null
        }

        /// <summary>
        /// Clear the panel before drawing on it (IMPORTANT)
        /// </summary>
        public static async Task<string> ClearPanel()
        {
            return await SendTunnelCommand("scene/panel/clear", JsonBuilder.GeneralPanelData(panelId));
        }

        /// <summary>
        /// Draw text on the Panel & Swap the buffer of the Panel (This updates the panel with the new text)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="positions"></param>
        public static async Task<string> UpdatePanelText(string text, double[] positions)
        {
            if (cameraNodeId != null && guidBike != null && panelId != null)
            {
                await ClearPanel();
                await SendTunnelCommand("scene/panel/drawtext", JsonBuilder.DrawTextOnPanelData(panelId, text, positions));
                return await SendTunnelCommand("scene/panel/swap", JsonBuilder.GeneralPanelData(panelId));
            }
            return null; // Error -> return null
        }

        /// <summary>
        /// Updates the speed of the Camera Node (-> parent of Bike node -> parent of Panel)
        /// </summary>
        /// <param name="newSpeed"></param>
        public static async void UpdateSpeed(double newSpeed)
        {
            if (cameraNodeId != null && guidBike != null && panelId != null)
            {
                // Update Panel Text with new speed
                await UpdatePanelText("Speed: " + newSpeed, new double[] { 20.0, 90.0 });
                // Update speed on Route
                await SendTunnelCommand("route/follow/speed", JsonBuilder.UpdateNodeSpeed(cameraNodeId, newSpeed/3));
            }
        }

        internal static bool IsConnected()
        {
            return isConnected; // Send connection status
        }
    }
}
