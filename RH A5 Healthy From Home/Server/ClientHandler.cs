using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server
{
    internal class ClientHandler
    {
        // EXAMPLE CODE:

        #region connection stuff
        private TcpClient tcpClient;
        private NetworkStream stream;
        private byte[] buffer = new byte[1024];
        private string totalBuffer = "";
        #endregion

        public string UserName { get; set; }

        public ClientHandler(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;

            this.stream = this.tcpClient.GetStream();
            stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnRead), null);
        }
        #region connection stuff
        private void OnRead(IAsyncResult ar)
        {
            try
            {
                int receivedBytes = stream.EndRead(ar);
                string receivedText = System.Text.Encoding.ASCII.GetString(buffer, 0, receivedBytes);
                totalBuffer += receivedText;
            }
            catch (IOException)
            {
                Server.Disconnect(this);
                return;
            }

            while (totalBuffer.Contains("\r\n\r\n"))
            {
                string packet = totalBuffer.Substring(0, totalBuffer.IndexOf("\r\n\r\n"));
                totalBuffer = totalBuffer.Substring(totalBuffer.IndexOf("\r\n\r\n") + 4);
                string[] packetData = Regex.Split(packet, "\r\n");
                handleData(packetData);
            }
            stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnRead), null);
        }
        #endregion

        private void handleData(string[] packetData)
        {
            Console.WriteLine($"Got a packet: {packetData[0]}");
            switch (packetData[0])
            {
                case "login":
                    if (!assertPacketData(packetData, 3))
                        return;
                    if (packetData[1] == packetData[2])
                    {
                        Write("login\r\nok");
                        this.UserName = packetData[1];
                    }
                    else
                        Write("login\r\nerror, wrong password");
                    break;
                case "chat":
                    {
                        if (!assertPacketData(packetData, 2))
                            return;
                        string message = $"{this.UserName} : {packetData[1]}";
                        Server.Broadcast($"chat\r\n{message}");
                        break;
                    }
                case "pm":
                    {
                        if (!assertPacketData(packetData, 3))
                            return;
                        string message = $"{this.UserName} : {packetData[2]}";
                        Server.SendToUser(packetData[1], message);
                        break;
                    }
            }


        }

        private bool assertPacketData(string[] packetData, int requiredLength)
        {
            if (packetData.Length < requiredLength)
            {
                Write("error");
                return false;
            }
            return true;
        }

        public void Write(string data)
        {
            var dataAsBytes = System.Text.Encoding.ASCII.GetBytes(data + "\r\n\r\n");
            stream.Write(dataAsBytes, 0, dataAsBytes.Length);
            stream.Flush();
        }
    }
}
