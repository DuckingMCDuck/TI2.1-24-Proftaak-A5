using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Server
    {
        // EXAMPLE CODE:
        //dit is example is door Daan gemaakt. Misschien moeten we gaan werken met Async. 

        //methodes die moeten komen van de klassendiagram zijn: 
        //-Update(string ConvertedData) en WriteToFile(String ConvertedData)

        private static TcpListener listener;
        private static List<ClientHandler> clients = new List<ClientHandler>();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Server!");

            listener = new TcpListener(IPAddress.Any, 15243);
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);

            Console.ReadLine();
        }

        private static void OnConnect(IAsyncResult ar)
        {
            var tcpClient = listener.EndAcceptTcpClient(ar);
            Console.WriteLine($"Client connected from {tcpClient.Client.RemoteEndPoint}");
            clients.Add(new ClientHandler(tcpClient));
            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);
        }

        internal static void Broadcast(string packet)
        {
            foreach (var client in clients)
            {
                client.Write(packet);
            }
        }

        internal static void Disconnect(ClientHandler client)
        {
            clients.Remove(client);
            Console.WriteLine("Client disconnected");
        }

        internal static void SendToUser(string user, string packet)
        {
            foreach (var client in clients.Where(c => c.UserName == user))
            {
                client.Write(packet);
            }
        }

        public void Update(string convertedData)
        {


        }

        public void WriteToFile(string convertedData) 
        { 
        
        }
    }
}
