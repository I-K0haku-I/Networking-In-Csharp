using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkingLearning.TcpChat
{
    public class Viewer
    {
        private readonly int BufferSize = 2 * 1024;
        private TcpClient _client;

        public bool Running { get; private set; }

        private bool _disconnectRequested = false;
        private readonly string ServerAddress;
        private int Port;
        private NetworkStream _msgStream = null;

        public Viewer(string serverAddress, int port)
        {
            _client = new TcpClient();
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            ServerAddress = serverAddress;
            Port = port;
        }


        public void Connect()
        {
            _client.Connect(ServerAddress, Port);  // resolves dns for us? // BLOCKS!!!!!!
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            if (_client.Connected)
            {
                Console.WriteLine("Connectd to the server at {0}.", endPoint);

                _msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes("viewer");
                _msgStream.Write(msgBuffer, 0, msgBuffer.Length);

                if (!Common.isDisconnected(_client))
                {
                    Running = true;
                    Console.WriteLine("Ctrl-C to exit.");
                }
                else
                {
                    Common.CleanupNetworkResources(_client);
                    Console.WriteLine("The server didn't recognize us as a Viewer.\n[");
                }
            }
            else
            {
                Common.CleanupNetworkResources(_client);
                Console.WriteLine("Wasn't able to connect to the server at {0}", endPoint);
            }
        }

        public void Disconnect()
        {
            Running = false;
            _disconnectRequested = true;
            Console.WriteLine("Disconnecting from the chat...");
        }

        // private void _cleanupNetworkResources()
        // {
        //     byte[] msgBuffer = Encoding.UTF8.GetBytes("BYE");
        //     _msgStream.Write(msgBuffer, 0, msgBuffer.Length); // BLOCKs!!!

        //     _msgStream?.Close();
        //     _msgStream = null;
        //     _client.Close();
        // }

        public void ListenForMessagesLoop()
        {
            bool wasRunning = Running;

            while (Running)
            {
                int messageLength = _client.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    _msgStream.Read(msgBuffer, 0, messageLength);

                    // copy pasterino for later:
                    // An alternative way of reading
                    //int bytesRead = 0;
                    //while (bytesRead < messageLength)
                    //{
                    //    bytesRead += _msgStream.Read(_msgBuffer,
                    //                                 bytesRead,
                    //                                 _msgBuffer.Length - bytesRead);
                    //    Thread.Sleep(1);    // Use less CPU
                    //}

                    string msg = Encoding.UTF8.GetString(msgBuffer);
                    Console.WriteLine(msg);
                }

                Thread.Sleep(10);

                if (Common.isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n[");
                }

                // seriously? appears overly complicated for no reason
                Running &= !_disconnectRequested;
            }

            Common.CleanupNetworkResources(_client);
            if (wasRunning)
                Console.WriteLine("Disconnected");
        }
    }
}
