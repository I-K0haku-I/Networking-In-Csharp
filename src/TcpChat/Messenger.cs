using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkingLearning.TcpChat
{
    public class Messenger
    {
        public readonly int BufferSize = 2 * 1024;
        private TcpClient _client;
        private NetworkStream _msgStream = null;

        public bool Running { get; private set; }

        public readonly string ServerAddress;
        public readonly int Port;
        public readonly string Name;

        public Messenger(string serverAddress, int port, string name)
        {
            _client = new TcpClient();
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            ServerAddress = serverAddress;
            Port = port;
            Name = name;
        }

        public void Connect()
        {
            _client.Connect(ServerAddress, Port);  // resolves dns for us? // BLOCKS!!!!!!
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            if (_client.Connected)
            {
                Console.WriteLine("Connected to the server at {0}.", endPoint);

                _msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes(String.Format("name:{0}", Name));
                _msgStream.Write(msgBuffer, 0, msgBuffer.Length);

                if (!Common.isDisconnected(_client))
                    Running = true;
                else
                {
                    Common.CleanupNetworkResources(_client);
                    Console.WriteLine("The server rejected us; \"{0}\" is probably in use.", Name);
                }
            }
            else
            {
                Common.CleanupNetworkResources(_client);
                Console.WriteLine("Wasn't able to connect to the server at {0}.", endPoint);
            }
        }

        public void SendMessagesLoop()
        {
            bool wasRunning = Running;

            while (Running)
            {
                Console.Write("{0}> ", Name);
                string msg = Console.ReadLine();

                if ((msg.ToLower() == "quit") || (msg.ToLower() == "exit"))
                {
                    Console.WriteLine("Disconnecting...");
                    Running = false;
                }
                else if (msg != string.Empty)
                {
                    byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
                    _msgStream.Write(msgBuffer, 0, msgBuffer.Length); // BLOCKS!!!!
                }

                Thread.Sleep(10);

                if (Common.isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n:[");
                }
            }

            Common.CleanupNetworkResources(_client);
            if (wasRunning)
                Console.WriteLine("Disconnected.");
        }

        // private void _cleanupNetworkResource()
        // {
        //     _msgStream?.Close();
        //     _msgStream = null;
        //     _client.Close();
        // }
    }
}
