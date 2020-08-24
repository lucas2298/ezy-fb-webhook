using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alliance.Auto.Bank.Client
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
    public class BankClient
    {
        // The port number for the remote device.  
        private const int port = 11000;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;
        private static string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Log_SocketClient");
        private static string Log;
        public static string StartClient()
        {
            if (!File.Exists(LogPath)) Directory.CreateDirectory(LogPath);
            var sMessage = string.Empty;
            Log = string.Empty;
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // The name of the
                // remote device is "host.contoso.com".  
                IPHostEntry ipHostInfo = Dns.Resolve("45.117.176.3");
                //IPHostEntry ipHostInfo = Dns.Resolve("127.0.0.1");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Log Ip, Port
                Log += "IP: " + ipAddress.ToString() + "\n";
                Log += "Port: " + port + "\n";

                // Create a TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                Log += "ConnectCallback\n";
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne(1000);
                if (Log.Contains("Error")) throw new Exception("Error occurred");
                
                // Send test data to the remote device.
                Log += "Send\n";
                Send(client, "This is a test<EOF>");
                sendDone.WaitOne(1000);

                // Receive the response from the remote device.
                Log += "Recieve\n";
                Receive(client);
                receiveDone.WaitOne(1000);

                // Write the response to the console.  
                Log += $"Response received : {response}" + "\n";
                //Console.WriteLine("Response received : {0}", response);

                // Release the socket.  
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                sMessage = e.Message;
                Log += "Error StartClient: " + sMessage + "\n";
            }
            if (Log.Contains("Error"))
            File.WriteAllText(LogPath + $@"\Log_{DateTime.Now.ToString("yyyyMMddhhmmss")}.txt", Log);
            return sMessage;
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            Log += "Start ConnectCallback\n";
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Log += $"Socket connected to {client.RemoteEndPoint.ToString()}\n";

                //Console.WriteLine("Socket connected to {0}",
                //    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Log += "Error ConnectCallback: " + e.Message + "\n";
                //Console.WriteLine(e.ToString());
            }
            Log += "Stop ConnectCallback\n";
        }

        private static void Receive(Socket client)
        {
            try
            {
                Log += "Start Receive\n";
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Log += "Error Receive: " + e.Message + "\n";
                Console.WriteLine(e.ToString());
            }
            Log += "End Receive\n";
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Log += "ReceiveCallback\n";
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Log += "Error ReceiveCallback: " + e.Message + "\n";
                //Console.WriteLine(e.ToString());
            }
            Log += "Stop ReceiveCallback\n";
        }

        private static void Send(Socket client, String data)
        {
            Log += "Start Send\n";
            Log += "Data: " + data + "\n";
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
            Log += "Stop Send\n";
        }

        private static void SendCallback(IAsyncResult ar)
        {
            Log += "Start SendCallback\n";
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Log += $"Sent {bytesSent} bytes to server.\n";
                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Log += "Error SendCallback: " + e.Message + "\n";
                //Console.WriteLine(e.ToString());
            }
            Log += "Stop SendCallback\n";
        }
    }
}
