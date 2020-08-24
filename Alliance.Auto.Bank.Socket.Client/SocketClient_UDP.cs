using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alliance.Auto.Bank.Client
{
    public class SocketClient_UDP
    {
        public struct Received
        {
            public IPEndPoint Sender;
            public string Message;
        }
        abstract class UdpBase
        {
            protected UdpClient Client;

            protected UdpBase()
            {
                Client = new UdpClient();
            }

            public async Task<Received> Receive()
            {
                var result = await Client.ReceiveAsync();
                return new Received()
                {
                    Message = Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length),
                    Sender = result.RemoteEndPoint
                };
            }
        }
        class UdpUser : UdpBase
        {
            private UdpUser() { }
            public static UdpUser ConnectTo(string hostname, int port)
            {
                var connection = new UdpUser();
                connection.Client.Connect(hostname, port);
                return connection;
            }
            public void Send(string message)
            {
                var datagram = Encoding.ASCII.GetBytes(message);
                var temp = Client.Send(datagram, datagram.Length);
            }
        }
        public static string StartClient()
        {
            string sMessage = string.Empty;
            //create a new client
            var client = UdpUser.ConnectTo("45.117.176.3", 11000);
            //var client = UdpUser.ConnectTo("127.0.0.1", 11000);
            //wait for reply messages from server and send them to console 
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        var received = await client.Receive();
                        if (received.Message.Contains("Reply"))
                        {
                            //System.Diagnostics.Debug.WriteLine("OKKKKKKK");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        sMessage = ex.Message;
                    }
                }
            });
            string read = "Send Signal";
            client.Send(read);
            return sMessage;
        }
    }
}
