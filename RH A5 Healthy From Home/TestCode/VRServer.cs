using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestCode
{
    internal class VRServer
    {
        public VRServer() 
        {
            // Create connection to VR server
            //IPAddress address = IPAddress.Parse("85.145.62.130");
            //IPEndPoint endPoint = new IPEndPoint(address, 6666);
            TcpClient vrServer = new TcpClient("85.145.62.130", 6666);


        }
    }
}
