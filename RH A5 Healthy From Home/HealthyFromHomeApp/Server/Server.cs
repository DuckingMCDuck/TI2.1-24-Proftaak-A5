using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HealthyFromHomeApp.Server
{
    internal class Server
    {
        private static TcpListener listener;
        private static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private static TcpClient doctorClient = null;

        public void Start()
        {
            Console.WriteLine("Server starting...");

            listener = new TcpListener(IPAddress.Any, 12345);
            listener.Start();
            Console.WriteLine("Server started and waiting for connections...");

            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);

            while (true)
            {
                Console.WriteLine("Press 'q' to quit.");
                if (Console.ReadLine()?.ToLower() == "q")
                {
                    Console.WriteLine("Server is shutting down.");
                    break;
                }
            }

            listener.Stop();
        }

        private static async void OnConnect(IAsyncResult ar)
        {
            TcpClient tcpClient = listener.EndAcceptTcpClient(ar);

            if (doctorClient == null)
            {
                doctorClient = tcpClient;
                Console.WriteLine("Doctor connected.");

                Task.Run(() => ListenForMessages(doctorClient, "Doctor", isDoctor: true));
            }
            else
            {
                await RegisterClient(tcpClient);
            }

            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);
        }

        private static async Task RegisterClient(TcpClient tcpClient)
        {
            string roleMessage = await ReceiveMessage(tcpClient);

            if (roleMessage.StartsWith("client:"))
            {
                string clientName = roleMessage.Substring("client:".Length);

                if (!clients.ContainsKey(clientName)) 
                {
                    clients.Add(clientName, tcpClient);
                    Console.WriteLine($"Client registered: {clientName}");

                    NotifyDoctorOfClients();
                    
                    Task.Run(() => ListenForMessages(tcpClient, clientName));
                }
            }
        }

        private static void NotifyDoctorOfClients()
        {
            if (doctorClient == null) return;

            string clientsList = string.Join(",", clients.Keys);
            SendMessage(doctorClient, $"clients_update:{clientsList}");
        }

        private static void ListenForMessages(TcpClient tcpClient, string senderName, bool isDoctor = false)
        {
            try
            {
                while (tcpClient.Connected) 
                {
                    string message = ReceiveMessage(tcpClient).Result;

                    if (message == null)
                    {
                        Console.WriteLine($"{senderName} disconnected.");
                        DisconnectClient(tcpClient, senderName); 
                        return;
                    }

                    Console.WriteLine($"Received message from {senderName}: {message}");

                    if (isDoctor)
                    {
                        ProcessDoctorMessage(message);
                    }
                    else
                    {
                        ProcessClientMessage(senderName, message);
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{senderName} connection error: {ex.Message}");
                DisconnectClient(tcpClient, senderName);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error with {senderName}: {ex.Message}");
                DisconnectClient(tcpClient, senderName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error with {senderName}: {ex.Message}");
                DisconnectClient(tcpClient, senderName);
            }
        }

        private static async Task<string> ReceiveMessage(TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[4096];

            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (byteCount == 0) return null; 

            return Encoding.ASCII.GetString(buffer, 0, byteCount).Trim();
        }

        private static void ProcessDoctorMessage(string message)
        {
            string[] splitMessage = message.Split(':');
            if (splitMessage[0] == "send_to")
            {
                string targetClient = splitMessage[1];
                string doctorMessage = string.Join(":", splitMessage.Skip(2));
                SendToClient(targetClient, $"Doctor: {doctorMessage}");
            }
        }

        private static void ProcessClientMessage(string clientName, string message)
        {
            if (message.StartsWith("chat:send_to:Doctor:"))
            {
                string clientMessage = message.Substring("chat:send_to:Doctor:".Length);
                if (doctorClient != null)
                {
                    SendMessage(doctorClient, $"{clientName}: {clientMessage}");
                }
            }
            else if (message.StartsWith("send_to:"))
            {
                Broadcast(message, clientName);
            }
        }

        internal static void Broadcast(string packet, string senderName)
        {
            string[] splitPacket = packet.Split(':');
            if (splitPacket[0] == "send_to")
            {
                string targetClient = splitPacket[1];
                string message = $"{senderName}: {string.Join(":", splitPacket.Skip(2))}";

                SendToClient(targetClient, message);
            }
        }

        internal static void SendToClient(string clientName, string message)
        {
            if (clients.TryGetValue(clientName, out TcpClient targetClient))
            {
                Console.WriteLine($"Sending message to {clientName} via TcpClient: {targetClient.Client.RemoteEndPoint}");
                SendMessage(targetClient, message);
            }
            else
            {
                Console.WriteLine($"Client {clientName} not found.");
            }
        }

        private static void SendMessage(TcpClient tcpClient, string message)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                NetworkStream stream = tcpClient.GetStream();
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message: {ex.Message}");
            }
        }

        private static void DisconnectClient(TcpClient tcpClient, string clientName)
        {
            if (clients.ContainsKey(clientName))
            {
                clients.Remove(clientName);
                Console.WriteLine($"Client {clientName} disconnected.");
                NotifyDoctorOfClients();
            }
        }

        public void Update(string convertedData)
        {

        }

        public async void WriteToFile(string convertedData)
        {
            string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamWriter outputToFile = new StreamWriter(Path.Combine(documentPath, "Async Data of Client")))
            {
                await outputToFile.WriteAsync(convertedData);
            }
        }
    }
}
