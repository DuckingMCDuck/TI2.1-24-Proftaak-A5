﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace Server
{
    internal class Server
    {
        // EXAMPLE CODE:
        //dit is example is door Daan gemaakt. Misschien moeten we gaan werken met Async. 

        //methodes die moeten komen van de klassendiagram zijn: 
        //-Update(string ConvertedData) en WriteToFile(String ConvertedData)

        // wat willen we gebruiken qua threadsafety iets makelijks zoals ConcurrentBag dan hoef je alleen de lijst zo te maken:
        // ConcurrentBag<ClientHandler> clients = new ConcurrentBag<ClientHandler>(); en in de Disconnect methode dit: clients = new ConcurrentBag<ClientHandler>(clients.Except(new[] { client })); 
        //voor de clients.remove(...) statement
        //of we kunnen lock gebruiken, dan moeten we alles in een lock gooien zoals dit:
        //private static readonly object clientsLock = new object();
        //  lock (clientsLock)
        //{
        //    clients.Add(new ClientHandler(tcpClient));
        //}



        private static TcpListener listener;
        private static List<TcpClient> clients = new List<TcpClient>();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Server!");

            listener = new TcpListener(IPAddress.Any, 15243);
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);

            while (true)
            {
                Console.WriteLine("Press 'q' to quit.");
                if (Console.ReadLine() == "q") break;
            }
        }

        private static void OnConnect(IAsyncResult ar)
        {
            TcpClient tcpClient = listener.EndAcceptTcpClient(ar);

            clients.Add(tcpClient);

            // tijdelijk: pak clientIP en port (moet ID worden of naam oid)
            string clientEndpoint = tcpClient.Client.RemoteEndPoint.ToString();

            Console.WriteLine($"Client connected: {clientEndpoint}");

            Console.WriteLine($"Total clients connected: {clients.Count}");

            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);
        }

        internal static void Broadcast(string packet)
        {
            byte[] data = Encoding.ASCII.GetBytes(packet);

            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send to client: {ex.Message}");
                }
            }
        }

        internal static void Disconnect(TcpClient client)
        {
            clients.Remove(client); 
            string clientEndpoint = client.Client.RemoteEndPoint.ToString();
            Console.WriteLine($"Client disconnected: {clientEndpoint}");
        }

        internal static void SendToUser(string user, string packet)
        {
            byte[] data = Encoding.ASCII.GetBytes(packet);
            foreach (var client in clients.Where(c => c.Client.RemoteEndPoint.ToString() == user))
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send to client: {ex.Message}");
                }
            }
        }

        public void Update(string convertedData)
        {


        }

        public async void WriteToFile(string convertedData) 
        {
            // is asynchroon, kan ook zonder.
            string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamWriter outputToFile = new StreamWriter(Path.Combine(documentPath, "Async Data of Client")))
            { 
                await outputToFile.WriteAsync(convertedData);
            }

        }
    }
}