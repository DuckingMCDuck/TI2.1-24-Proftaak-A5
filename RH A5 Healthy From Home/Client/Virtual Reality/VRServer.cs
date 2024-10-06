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
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml.Linq;

namespace Client
{
    internal class VRServer
    {
        public static TcpClient vrServer; //TcpClient?
        public static NetworkStream stream; //NetworkStream?
        public static string receivedData;
        public static byte[] prepend;
        public static byte[] data;
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
            vrServer = new TcpClient("85.145.62.130", 6666);
            stream = vrServer.GetStream();
            MainWindow.TextChat.Text = "Connected to VR Server!\n";

            // Start listening for packets
            await ListenForPacketsAsync();
        }

        /// <summary>
        /// Handles data from incoming packets and sent packets
        /// </summary>
        /// <returns></returns>
        public static async Task ListenForPacketsAsync()
        {
            // Get the sessionID
            MainWindow.TextBoxBikeData.Text = "Sending starting packet...";
            SendStartingPacket();
            string sessionId = await ReceivePacketAsync();
            Console.WriteLine($"Received session ID: {sessionId}");

            if (!string.IsNullOrEmpty(sessionId))
            {
                // Get the tunnelID
                MainWindow.TextBoxBikeData.Text = "Sending session ID packet...";
                SendSessionIdPacket(sessionId);
                string tunnelId = await ReceivePacketAsync();
                Console.WriteLine($"Received tunnel ID: {tunnelId}");

                if (!string.IsNullOrEmpty(tunnelId))
                {
                    // Send scene configuration commands
                    //while (vrServer.Connected)
                    {
                        // Reset scene:
                        //SendTunnelCommand(tunnelId, "scene/reset", "{}");

                        // SkyBox time & update (dynamic works, static doesn't):
                        SendTunnelCommand(tunnelId, "scene/skybox/settime", "{ time : 23 }");
                        //SendTunnelCommand(tunnelId, "scene/skybox/update", "{\"type\" : \"dynamic\",\"files\" : {}}");
                        //SendTunnelCommand(tunnelId, "scene/skybox/update", "{\"type\" : \"static\",\"files\" : {\"xpos\" : \"data/NetworkEngine/textures/SkyBoxes/interstellar/interstellar_rt.png\",\"xneg\" : \"data/NetworkEngine/textures/SkyBoxes/interstellar/interstellar_lf.png\",\"ypos\" : \"data/NetworkEngine/textures/SkyBoxes/interstellar/interstellar_up.png\",\"yneg\" : \"data/NetworkEngine/textures/SkyBoxes/interstellar/interstellar_dn.png\",\"zpos\" : \"data/NetworkEngine/textures/SkyBoxes/interstellar/interstellar_bk.png\",\"zneg\" : \"data/NetworkEngine/textures/SkyBoxes/interstellar/interstellar_ft.png\"} }");

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
            byte[] dataBuffer = new byte[dataLength];
            totalBytesRead = 0;

            // Extract other data from the buffer
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
            string dataString = Encoding.UTF8.GetString(dataBuffer);
            Debug.WriteLine("Received data: " + dataString);
            Console.WriteLine("received data: " + dataString);

            return GetId(dataString);
        }

        /// <summary>
        /// Gets the id from incoming data (sessionId, tunnelId etc.)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetId(string data)
        {
            var jsonDocument = JsonDocument.Parse(data);

            // The id can be inside of an array or object, so we have 2 cases:
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
                return jsonNode["id"].GetValue<string>();
            }
            return null;
        }

        /// <summary>
        /// Initialize starting packet
        /// </summary>
        public static void SendStartingPacket()
        {
            string jsonPacket = "{\"id\" : \"session/list\"}";
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }

        /// <summary>
        /// Initialize session packet
        /// </summary>
        /// <param name="sessionId"></param>
        public static void SendSessionIdPacket(string sessionId)
        {
            string jsonPacket = $"{{\"id\" : \"tunnel/create\",\"data\" : {{\"session\" : \"{sessionId}\",\"key\" : \"\"}}}}";
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }

        /// <summary>
        /// Initialize a tunnel command (Generic command for all tunnel functions)
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="command"></param>
        /// <param name="commandData"></param>
        public static void SendTunnelCommand(string tunnelId, string command, string commandData)
        {
            string jsonPacket = $"{{\"id\" : \"tunnel/send\",\"data\" :{{\"dest\" : \"{tunnelId}\",\"data\" : {{\"id\" : \"{command}\",\"data\" : {commandData}}}}}}}";
            byte[] data = Encoding.ASCII.GetBytes(jsonPacket);
            byte[] prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }


        // OLD CODE:
        ///// <summary>
        ///// Receive packet from the server and print it to the console
        ///// </summary>
        //public static void ReceivePacket()
        //{
        //    // Get prepend byte array from server
        //    byte[] prependBuffer = new byte[4];
        //    int totalBytesRead = 0;

        //    while (totalBytesRead < 4)
        //    {
        //        int bytesRead = stream.Read(prependBuffer, totalBytesRead, 4 - totalBytesRead);
        //        if (bytesRead == 0)
        //        {
        //            Console.WriteLine("Error: Connection closed before reading the full length.");
        //            return;
        //        }
        //        totalBytesRead += bytesRead;
        //    }
        //    int dataLength = BitConverter.ToInt32(prependBuffer, 0);
        //    Debug.WriteLine("amount " + dataLength.ToString());

        //    //int bytesRead = 0;
        //    //byte[] readBuffer = new byte[1500];
        //    //string totalResponse = "";

        //    //// Continue reading if not all bytes are sent in one packet
        //    //while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
        //    //{
        //    //    int response = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
        //    //    string responseText = Encoding.ASCII.GetString(readBuffer, 0, response);
        //    //    totalResponse += responseText;
        //    //    bytesToRead += bytesRead;
        //    //}
        //    //int prependByte = stream.ReadByte();
        //    //prependBuffer[0] = prependByte;
        //    //int prepend = await stream.ReadAsync(prependBuffer, 0, prependBuffer.Length);
        //    //byte[] p = stream.Read(prependBuffer, 0, prependBuffer.Length);
        //    //string prependText = Encoding.UTF32.GetString(prependBuffer, 0, prepend);

        //    //int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
        //    //string response = Encoding.ASCII.GetString(buffer, 0, bytes);
        //    //totalResponse = response;

        //    //while (bytes < buffer.Length)
        //    //{
        //    //    int addedBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
        //    //    response = Encoding.ASCII.GetString(buffer, 0, addedBytes);
        //    //    totalResponse += response;
        //    //    bytes += addedBytes;
        //    //}

        //    //// Print for Debugging
        //    //Console.WriteLine("total response: " + totalResponse);
        //    //receivedData = totalResponse;
        //}
    }
}
