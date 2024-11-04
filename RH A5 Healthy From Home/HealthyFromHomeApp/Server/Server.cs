using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HealthyFromHomeApp.Common;
using System.Windows.Documents.Serialization;

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

            listener.BeginAcceptTcpClient(OnConnect, null);

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

            listener.BeginAcceptTcpClient(OnConnect, null);

            await Task.Run(() => RegisterClient(tcpClient));
        }

        private static async Task RegisterClient(TcpClient tcpClient)
        {
            string message = await ReceiveMessage(tcpClient);

            if (message.StartsWith("login:"))
            {
                string[] credentials = message.Substring("login:".Length).Split(':');
                string username = credentials[0];
                string password = credentials[1];

                if (ValidateDoctorCredentials(username, password))
                {
                    if (doctorClient == null)  
                    {
                        doctorClient = tcpClient;
                        Console.WriteLine("Doctor logged in successfully.");

                        SendMessage(doctorClient, "login_success");

                        NotifyDoctorOfClients();
                        Task.Run(() => ListenForMessages(doctorClient, "Doctor", isDoctor: true));
                    }
                    else
                    {
                        SendMessage(tcpClient, "login_failure");
                        tcpClient.Close(); 
                        Console.WriteLine("Doctor login attempt rejected: another doctor is already connected.");
                    }
                }
                else
                {
                    SendMessage(tcpClient, "login_failure"); 
                    tcpClient.Close();
                }
            }
            else if (message.StartsWith("client:"))
            {
                string clientName = message.Substring("client:".Length);

                if (!clients.ContainsKey(clientName))
                {
                    clients.Add(clientName, tcpClient);
                    Console.WriteLine($"Client registered: {clientName}");

                    NotifyDoctorOfClients();

                    Task.Run(() => ListenForMessages(tcpClient, clientName));
                }
                else
                {
                    SendMessage(tcpClient, "client_registration_failed");
                    tcpClient.Close();
                }
            }
            else
            {
                Console.WriteLine("Invalid connection message received, closing the connection.");
                tcpClient.Close();
            }
        }

        private static bool ValidateDoctorCredentials(string username, string password)
        {
            // hardcoded voor nu
            return username == "doc" && password == "lol";
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

                    WriteToFile(message, senderName);

                    Console.WriteLine($"Received message from {senderName}: {message}");

                    ProcessMessage(senderName, message, isDoctor);
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

        public static async void WriteToFile(string convertedData, string clientName)
        {
            string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamWriter outputToFile = new StreamWriter(Path.Combine(documentPath, $"{clientName}_data.txt"),true))
            {
                await outputToFile.WriteAsync(convertedData);
            }
        }
        private static async Task<string> ReceiveMessage(TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[4096];

            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (byteCount == 0) return null;

            string encryptedMessage = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();
            string decrypted = EncryptHelper.Decrypt(encryptedMessage);
            return decrypted;
        }

        private static void ProcessMessage(string senderName, string message, bool isDoctor)
        {
            string[] splitMessage = message.Split(':');

            if (splitMessage[0] == "broadcast" && isDoctor)
            {
                string broadcastMessage = string.Join(":", splitMessage.Skip(1));
                BroadcastMessageToAllClients($"Doctor (Broadcast): {broadcastMessage}");
            }
            else if (splitMessage[0] == "send_to")
            {
                string targetClient = splitMessage[1];
                string actualMessage = string.Join(":", splitMessage.Skip(2));
                SendToClient(targetClient, $"{(isDoctor ? "Doctor" : senderName)}: {actualMessage}");
            }
            else if (!isDoctor && message.StartsWith("chat:send_to:Doctor:"))
            {
                string clientMessage = message.Substring("chat:send_to:Doctor:".Length);
                ForwardToDoctor(senderName, clientMessage);
            }
            else if (!isDoctor && message.StartsWith("bike_data:"))
            {
                ForwardToDoctor(senderName, message);
            }
        }

        private static void BroadcastMessageToAllClients(string message)
        {
            foreach (var client in clients.Values)
            {
                SendMessage(client, message);
            }
        }

        private static void ForwardToDoctor(string clientName, string message)
        {
            if (doctorClient != null)
            {
                SendMessage(doctorClient, $"{clientName}: {message}");
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
                string encryptedMessage = EncryptHelper.Encrypt(message);
                byte[] data = Encoding.UTF8.GetBytes(encryptedMessage);
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
            if (clients.Remove(clientName))
            {
                Console.WriteLine($"Client {clientName} disconnected.");
                NotifyDoctorOfClients();
            }
        }
        public static async void WriteToFile(string convertedData, string clientName)
        {
            string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamWriter outputToFile = new StreamWriter(Path.Combine(documentPath, $"{clientName}_data.txt")))
            {
                await outputToFile.WriteAsync(convertedData);
            }
        }
    }
}