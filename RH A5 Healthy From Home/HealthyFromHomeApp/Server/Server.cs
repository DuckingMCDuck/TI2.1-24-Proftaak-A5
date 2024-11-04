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
using Client.Virtual_Reality;

namespace HealthyFromHomeApp.Server
{
    internal class Server
    {
        // Listener to accept incoming connections, clients dictionary to track connected clients, doctor client as a unique connection
        private static TcpListener listener;
        private static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private static TcpClient doctorClient = null;

        // Start method initializes the server and waits for incoming connections
        public void Start()
        {
            Console.WriteLine("Server starting...");

            // Start listening on port 12345 for client connections
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

        // Async handler for incoming connections
        private static async void OnConnect(IAsyncResult ar)
        {
            TcpClient tcpClient = listener.EndAcceptTcpClient(ar);

            listener.BeginAcceptTcpClient(OnConnect, null);

            await Task.Run(() => RegisterClient(tcpClient)); // Process new client asynchronously
        }

        // Method to register a client by reading its initial message to determine type (doctor or regular client)
        private static async Task RegisterClient(TcpClient tcpClient)
        {
            string message = await ReceiveMessage(tcpClient);

            // Handle doctor login
            if (message.StartsWith("login:"))
            {
                string[] credentials = message.Substring("login:".Length).Split(':');
                string username = credentials[0];
                string password = credentials[1];

                if (ValidateDoctorCredentials(username, password))
                {
                    if (doctorClient == null)  // Only allow one doctor to connect
                    {
                        doctorClient = tcpClient;
                        Console.WriteLine("Doctor logged in successfully.");

                        SendMessage(doctorClient, "login_success");

                        NotifyDoctorOfClients(); // Send client list to the doctor
                        Task.Run(() => ListenForMessages(doctorClient, "Doctor", isDoctor: true));
                    }
                    else
                    {
                        SendMessage(tcpClient, "login_failure");
                        tcpClient.Close(); // Close connection if another doctor is already connected
                        Console.WriteLine("Doctor login attempt rejected: another doctor is already connected.");
                    }
                }
                else
                {
                    SendMessage(tcpClient, "login_failure"); 
                    tcpClient.Close();
                }
            }
            // Handle client registration
            else if (message.StartsWith("client:"))
            {
                string clientName = message.Substring("client:".Length);

                if (!clients.ContainsKey(clientName))
                {
                    clients.Add(clientName, tcpClient);
                    Console.WriteLine($"Client registered: {clientName}");

                    NotifyDoctorOfClients(); // Notify doctor of new client

                    Task.Run(() => ListenForMessages(tcpClient, clientName));
                }
                else
                {
                    SendMessage(tcpClient, "client_registration_failed"); // Name conflict
                    tcpClient.Close();
                }
            }
            else
            {
                Console.WriteLine("Invalid connection message received, closing the connection.");
                tcpClient.Close();
            }
        }

        // Hardcoded credentials validation for the doctor
        private static bool ValidateDoctorCredentials(string username, string password)
        {
            return username == "doc" && password == "lol";
        }

        // Send an updated list of clients to the doctor
        private static void NotifyDoctorOfClients()
        {
            if (doctorClient == null) return;

            string clientsList = string.Join(",", clients.Keys);
            SendMessage(doctorClient, $"clients_update:{clientsList}");
        }

        // Main method to listen for messages from either the doctor or clients
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

                    ProcessMessage(senderName, message, isDoctor); // Process the received message
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

        // Method to write data to a file with client's name
        public static async void WriteToFile(string convertedData, string clientName)
        {

            string projectPath = AppDomain.CurrentDomain.BaseDirectory;
            string clientDataPath = Path.Combine(projectPath, "ClientData");

            //create clientData folder if not exists
            if (!Directory.Exists(clientDataPath)) 
            { 
                Directory.CreateDirectory(clientDataPath); 
            }

            string filePath = Path.Combine(clientDataPath, $"{clientName}_data.txt");
            try
            {
                using (StreamWriter outputToFile = new StreamWriter(filePath, true))
                {
                    await outputToFile.WriteAsync(convertedData);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e + "fault at StreamWriter");
            }
        }

        // Async method to receive a message from a client

        public static async Task<string> ReadFileOfClient(string clientName) 
        {
            string projectPath = AppDomain.CurrentDomain.BaseDirectory;
            string clientDataPath = Path.Combine(projectPath, "ClientData"); 
            string filePath = Path.Combine(clientDataPath, $"{clientName}_data.txt");

            if (!File.Exists(Path.Combine(projectPath,filePath)))
            {
                Console.WriteLine($"File not found: " + $"{filePath}"); 
                return null;
            }
            try
            {
                using (StreamReader streamReader = new StreamReader(filePath))
                {
                    int chuckSize = 1500;
                    char[] buffer = new char[chuckSize];
                    int bytesRead = await streamReader.ReadAsync(buffer,0,chuckSize);
                    string content = new string(buffer, 0, bytesRead);
                    Console.WriteLine($"File content read: {content}");
                    return "file_content:" + content;
                }
            }
            catch (IOException e) 
            {
                Console.WriteLine(e + "fault at StreamRider");
                return null;
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

        // Process message based on sender and content type (broadcast, direct message, or chat to doctor)
        public static async Task SendFileInChunks(string clientName)
        {
            string projectPath = AppDomain.CurrentDomain.BaseDirectory;
            string clientDataPath = Path.Combine(projectPath, "ClientData");
            string filePath = Path.Combine(clientDataPath, $"{clientName}_data.txt");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                ForwardFileToDoctor(clientName, $"{clientName}:error:File not found.");
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    int chunkSize = 1000;
                    char[] buffer = new char[chunkSize];
                    int bytesRead;
                    int chunkNumber = 1;

                    // Read and send file in 1000 character chunks
                    while ((bytesRead = await reader.ReadAsync(buffer, 0, chunkSize)) > 0)
                    {
                        string chunkContent = new string(buffer, 0, bytesRead);
                        string message = $"file_content:{clientName}:file_chunk:{chunkNumber}:{chunkContent}";

                        // Forward each chunk message to the doctor
                        ForwardFileToDoctor(clientName, message);
                        chunkNumber++;
                    }

                    // Notify that file sending is complete
                    ForwardFileToDoctor(clientName, $"{clientName}:file_transfer_complete");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error reading file for {clientName}: {e.Message}");
            }
        }

        private static void ProcessMessage(string senderName, string message, bool isDoctor)
        {
            string[] splitMessage = message.Split(':');

            // Broadcast from doctor to all clients
            if (splitMessage[0] == "broadcast" && isDoctor)
            {
                string broadcastMessage = string.Join(":", splitMessage.Skip(1));
                BroadcastMessageToAllClients($"Doctor (Broadcast): {broadcastMessage}");
            }
            // Direct message to a specific client
            else if (splitMessage[0] == "send_to")
            {
                string targetClient = splitMessage[1];
                string actualMessage = string.Join(":", splitMessage.Skip(2));
                SendToClient(targetClient, $"{(isDoctor ? "Doctor" : senderName)}: {actualMessage}");
            }
            else if (isDoctor && message.StartsWith("request_file:")) 
            {
                string clientFileName = message.Substring("request_file:".Length);
                Console.WriteLine($"Requested file: {clientFileName}");

                Task.Run(() => SendFileInChunks(clientFileName));
            }
            // Message from client to doctor
            else if (!isDoctor && message.StartsWith("chat:send_to:Doctor:"))
            {
                string clientMessage = message.Substring("chat:send_to:Doctor:".Length);
                ForwardToDoctor(senderName, clientMessage);
            }
            // Forward bike data from client to doctor
            else if (!isDoctor && message.StartsWith("bike_data:"))
            {
                WriteToFile(message, senderName);
                ForwardToDoctor(senderName, message);
            }
        }

        // Broadcast message to all connected clients
        private static void BroadcastMessageToAllClients(string message)
        {
            foreach (var client in clients.Values)
            {
                SendMessage(client, message);
            }
        }

        // Forward a message from a client to the doctor
        private static void ForwardToDoctor(string clientName, string message)
        {
            if (doctorClient != null)
            {
                SendMessage(doctorClient, $"{clientName}: {message}");
            }
        }

        private static void ForwardFileToDoctor(string clientName, string message)
        {
            if (doctorClient != null)
            {
                SendMessage(doctorClient, $"{message}");
            }
        }

        // Send message to a specific client by name
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

        // Send encrypted message to a specific TcpClient
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

        // Handle client disconnection and notify the doctor
        private static void DisconnectClient(TcpClient tcpClient, string clientName)
        {
            if (clients.Remove(clientName))
            {
                Console.WriteLine($"Client {clientName} disconnected.");
                NotifyDoctorOfClients();
            }
        }
    }
}