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
            bool didSubmit = false;
            string inputMsg = "";
            Console.Write("{0}> ", Name);

            while (Running)
            {
                // string inputMsg = Console.ReadLine();  // does this block? yes it does!

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            didSubmit = true;
                            break;
                        default:
                            inputMsg += key.KeyChar.ToString();
                            Console.Write(key.KeyChar);
                            break;
                    }
                }

                if (didSubmit)
                {
                    if ((inputMsg.ToLower() == "quit") || (inputMsg.ToLower() == "exit"))
                    {
                        Console.WriteLine("Disconnecting...");
                        Running = false;
                    }
                    else if (inputMsg != string.Empty)
                    {
                        byte[] msgBuffer = Encoding.UTF8.GetBytes(inputMsg);
                        _msgStream.Write(msgBuffer, 0, msgBuffer.Length); // BLOCKS!!!!
                    }
                    didSubmit = false;
                    inputMsg = "";
                    Console.WriteLine("");
                    Console.Write("{0}> ", Name);
                }

                int messageLength = _client.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    _msgStream.Read(msgBuffer, 0, messageLength);

                    string msg = Encoding.UTF8.GetString(msgBuffer);

                    if (msg.StartsWith("STATUS:"))
                    {
                        string statusMsg = msg.Substring(msg.IndexOf(':') + 1);
                        if (statusMsg == "PING")
                        {
                            byte[] responseBuffer = Encoding.UTF8.GetBytes("STATUS:PONG");
                            _msgStream.Write(responseBuffer, 0, responseBuffer.Length);
                        }
                    }
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
