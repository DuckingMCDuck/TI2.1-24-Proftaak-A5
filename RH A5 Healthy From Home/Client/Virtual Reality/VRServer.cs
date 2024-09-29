﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class VRServer
    {
        public VRServer() 
        {
            // Establish a connection to the Vr server
            using (TcpClient vsServer = new TcpClient())
            {
                MainWindow.client.chatText = "Connected to VR Server!\n";
                vsServer.ConnectAsync("85.145.62.130", 6666);

                // TODO: This is ChatGPT code, NetworkStream is not working properly
                while (true)
                {
                    using (NetworkStream stream = vsServer.GetStream())
                    {
                        MainWindow.client.chatText = "Sending packets...";
                        // Prepare the JSON packet as a byte array
                        string jsonPacket = "{\"id\":\"session/list\"}";
                        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonPacket);

                        // Calculate the total length of the packet (27 bytes for the JSON string in this case)
                        int packetLength = jsonBytes.Length;

                        // Prepare the header: 4 bytes in big-endian format for the packet length
                        byte[] packetHeader = BitConverter.GetBytes(packetLength);

                        // Convert the length to big-endian (VrLib requires big-endian order)
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(packetHeader);
                        }

                        // Create the final packet to send (header + JSON data)
                        byte[] finalPacket = new byte[packetHeader.Length + jsonBytes.Length];
                        Array.Copy(packetHeader, 0, finalPacket, 0, packetHeader.Length);
                        Array.Copy(jsonBytes, 0, finalPacket, packetHeader.Length, jsonBytes.Length);

                        // Send the packet to the server
                        MainWindow.client.chatText = "Sending packet...";
                        stream.WriteAsync(finalPacket, 0, finalPacket.Length);

                        MainWindow.client.chatText = "Packet sent. Waiting for response...";

                        // Optional: Read response from the server
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MainWindow.client.chatText = "Received response: " + response;
                    }
                }
            }
        }
    }
}
