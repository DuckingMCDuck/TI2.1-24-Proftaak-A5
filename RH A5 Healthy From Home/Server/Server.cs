using System;
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
