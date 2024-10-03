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
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml.Linq;

namespace Client
{
    internal class VRServer
    {
        public static TcpClient vrServer = new TcpClient();
        public static NetworkStream stream;
        public static string receivedData;
        public static byte[] prepend;
        public static byte[] data;
        public VRServer()
        {
            // Establish a connection to the VR server
            vrServer.Connect("85.145.62.130", 6666);
            stream = vrServer.GetStream();
            MainWindow.client.chatText = "Connected to VR Server!\n";

            receivedData = "";
            //string recievedDataPart1 = "";
            //string recievedDataPart2 = "";
            //string combinedData = "";
            //string data = "";
            //string command = "";
            //string commandData = "";

            while (vrServer.Connected)
            {
                // Send packets to & get data from the server:
                SendStartingPacket();

                ReceivePacket();
                //// Always listen for incoming packets
                //Task.Run(() =>
                //{
                //    while (true)
                //    {
                //        ReceivePacket();
                //    }
                //});

                //recievedDataPart1 = ReceivePacket();
                //recievedDataPart2 = ReceivePacket();
                //combinedData = recievedDataPart1 + recievedDataPart2;
                //SendSessionIdPacket(combinedData);

                //data = ReceivePacket();
                //command = "scene/skybox/settime";
                //commandData = "time : 24";
                //SendTunnelCommand(data, command, commandData);

                //data = ReceivePacket();
                //command = "scene/skybox/update";
                //commandData = "\"type\" : \"static\",\"files\" : {}}";
                //SendSkyboxUpdate(command, commandData);

                //data = ReceivePacket(stream);

                //try
                //{
                //    // Step 3: Parse the JSON
                //    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                //    {
                //        JsonElement root = doc.RootElement;
                //        // Navigate to the "data" array
                //        if (root.TryGetProperty("data", out JsonElement dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                //        {
                //            // Ensure the array is not empty
                //            if (dataArray.GetArrayLength() > 0)
                //            {
                //                JsonElement firstItem = dataArray[0];
                //                // Extract the "id" property
                //                if (firstItem.TryGetProperty("id", out JsonElement idElement))
                //                {
                //                    string extractedId = idElement.GetString();
                //                    Console.WriteLine("Extracted ID: " + extractedId);
                //                }
                //                else
                //                {
                //                    Console.WriteLine("'id' property not found in the first item of the 'data' array.");
                //                }
                //            }
                //            else
                //            {
                //                Console.WriteLine("The 'data' array is empty.");
                //            }
                //        }
                //        else
                //        {
                //            Console.WriteLine("'data' array not found in the JSON.");
                //        }
                //    }
                //}
                //catch (System.Text.Json.JsonException ex)
                //{
                //    Console.WriteLine("Error parsing JSON: " + ex.Message);
                //}
            }
        }

        /// <summary>
        /// Send packet to VR server
        /// </summary>
        /// <param name="prepend"></param>
        /// <param name="data"></param>
        public static void SendPacket(byte[] prepend, byte[] data)
        {
            byte[] combinedArray = new byte[prepend.Length + data.Length];
            Array.Copy(prepend, 0, combinedArray, 0, prepend.Length);
            Array.Copy(data, 0, combinedArray, prepend.Length, data.Length);
            stream.Write(combinedArray, 0, combinedArray.Length);
        }

        /// <summary>
        /// Receive packet from the server and print it to the console
        /// </summary>
        public static void ReceivePacket()
        {
            // Get prepend byte array from server
            byte[] prependBuffer = new byte[4];
            int totalBytesRead = 0;

            while (totalBytesRead < 4)
            {
                int bytesRead = stream.Read(prependBuffer, totalBytesRead, 4 - totalBytesRead);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Error: Connection closed before reading the full length.");
                    return;
                }
                totalBytesRead += bytesRead;
            }
            int dataLength = BitConverter.ToInt32(prependBuffer, 0);
            Debug.WriteLine("amount " + dataLength.ToString());
           

            //int bytesRead = 0;
            //byte[] readBuffer = new byte[1500];
            //string totalResponse = "";

            //// Continue reading if not all bytes are sent in one packet
            //while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
            //{
            //    int response = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
            //    string responseText = Encoding.ASCII.GetString(readBuffer, 0, response);
            //    totalResponse += responseText;
            //    bytesToRead += bytesRead;
            //}
            //int prependByte = stream.ReadByte();
            //prependBuffer[0] = prependByte;
            //int prepend = await stream.ReadAsync(prependBuffer, 0, prependBuffer.Length);
            //byte[] p = stream.Read(prependBuffer, 0, prependBuffer.Length);
            //string prependText = Encoding.UTF32.GetString(prependBuffer, 0, prepend);

            //int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            //string response = Encoding.ASCII.GetString(buffer, 0, bytes);
            //totalResponse = response;

            //while (bytes < buffer.Length)
            //{
            //    int addedBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            //    response = Encoding.ASCII.GetString(buffer, 0, addedBytes);
            //    totalResponse += response;
            //    bytes += addedBytes;
            //}

            //// Print for Debugging
            //Console.WriteLine("total response: " + totalResponse);
            //receivedData = totalResponse;
        }

        /// <summary>
        /// Get the id (value after the second '{' character) from the recieved data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetId(string data)
        {
            // Extract sessionId from incoming data
            int jsonStartIndex = 0;
            for (int i = 0; i < 2; i++)
            {
                jsonStartIndex = data.IndexOf('{');
                if (jsonStartIndex == -1)
                {
                    return "";
                }
                data = data.Substring(jsonStartIndex + 1);
            }
            string[] arrayToFindSessionId = data.Split('"');
            string id = data.Substring(6, arrayToFindSessionId[3].Length);
            return id;
        }

        public static void SendStartingPacket()
        {
            // Prepare the JSON packet as a byte array
            string jsonPacket = "{\"id\" : \"session/list\"}";
            data = Encoding.ASCII.GetBytes(jsonPacket);
            prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }

        public static void SendSessionIdPacket(string theData)
        {
            string sessionId = GetId(theData);
            if (sessionId.Equals(""))
            {
                return;
            }
            data = Encoding.ASCII.GetBytes($"{{\"id\" : \"tunnel/create\",\"data\" : {{\"session\" : \"{sessionId}\",\"key\" : \"\"}}}}");
            prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }

        public static void SendTunnelCommand(string theData, string command, string commandData)
        {
            string tunnelId = GetId(theData);
            data = Encoding.ASCII.GetBytes($"{{\"id\" : \"tunnel/send\",\"data\" :{{\"dest\" : \"{tunnelId}\",\"data\" : {{\"id\" : \"{command}\",\"data\" : {commandData}}}}}}}");
            prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }

        public static void SendSkyboxUpdate(string command, string commandData)
        {
            data = Encoding.ASCII.GetBytes($"{{\"id\" : \"scene/skybox/update\",\"data\" : {{\"type\" : \"dynamic\"}}");
            prepend = new byte[] { (byte)data.Length, 0x00, 0x00, 0x00 };
            SendPacket(prepend, data);
        }
    }
}
