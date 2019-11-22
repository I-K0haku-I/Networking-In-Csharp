using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace NetworkingLearning.TcpChat
{
    public enum ClientType
    {
        Messenger,
        Viewer
    }



    public class Server
    {
        private readonly string ChatName;
        private readonly int Port;
        public readonly int BufferSize = 2 * 1024;
        private readonly int maxMissedPongs = 3;

        public bool Running { get; private set; }

        private TcpListener _listener;
        private List<TcpClient> _viewers = new List<TcpClient>();
        private List<TcpClient> _messengers = new List<TcpClient>();
        private Dictionary<TcpClient, string> _names = new Dictionary<TcpClient, string>();
        private Queue<string> _messageQueue = new Queue<string>();
        private Queue<(TcpClient, string)> _statusMessageQueue = new Queue<(TcpClient, string)>();
        private List<TcpClient> _clientsSaidBye = new List<TcpClient>();
        private Dictionary<TcpClient, int> _missedPongCounter = new Dictionary<TcpClient, int>();

        public Server(string chatName, int port)
        {
            ChatName = chatName;
            Port = port;
            Running = false;

            _listener = new TcpListener(IPAddress.Any, Port);

        }

        public void Shutdown()
        {
            Running = false;
            Console.WriteLine("Shutting down server");
        }

        public void Run()
        {
            Console.WriteLine("Starting \"{0}\" TCP Chat Server on port {1}", ChatName, Port);
            Console.WriteLine("Ctr-C to quit.");

            SimpleTimer pingTimer = new SimpleTimer(_handlePing, 3000);
            _listener.Start();
            Running = true;

            while (Running)
            {
                if (_listener.Pending())
                    _handleNewConnection();

                _checkForDisconnects();

                foreach (TcpClient m in _messengers)
                    _checkForClientLoad(m, ClientType.Messenger);
                foreach (TcpClient m in _viewers)
                    _checkForClientLoad(m, ClientType.Viewer);

                _processQueues();

                pingTimer.Tick();
                Thread.Sleep(10);
            }

            foreach (TcpClient v in _viewers)
                _cleanupClient(v);
            foreach (TcpClient v in _messengers)
                _cleanupClient(v);
            _listener.Stop();

            Console.WriteLine("Server successfully shut down");
        }

        private void _handlePing()
        {
            Console.WriteLine("HandlingPing");
            byte[] msgBuffer = Encoding.UTF8.GetBytes("STATUS:PING");

            foreach (TcpClient v in _viewers)
            {
                v.GetStream().Write(msgBuffer, 0, msgBuffer.Length);
                if (!_missedPongCounter.ContainsKey(v))
                    _missedPongCounter.Add(v, 1);
                else
                    _missedPongCounter[v] += 1;
            }
            foreach (TcpClient m in _messengers)
            {
                m.GetStream().Write(msgBuffer, 0, msgBuffer.Length);
                if (!_missedPongCounter.ContainsKey(m))
                    _missedPongCounter.Add(m, 1);
                else
                    _missedPongCounter[m] += 1;
            }
        }

        private void _cleanupClient(TcpClient client)
        {
            _missedPongCounter.Remove(client);
            _clientsSaidBye.Remove(client);
            client.GetStream().Close();
            client.Close();
        }

        private void _processQueues()
        {
            foreach (string msg in _messageQueue)
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);

                foreach (TcpClient v in _viewers)
                {
                    v.GetStream().Write(msgBuffer, 0, msgBuffer.Length);
                }
            }
            _messageQueue.Clear();

            foreach ((TcpClient client, string msg) in _statusMessageQueue)
            {
                if (msg == "BYE")
                {
                    Console.WriteLine("{0} said BYE", _names[client]);  // potential crash when bye from someone who is not connected
                    _clientsSaidBye.Add(client);
                }
                else if (msg == "PONG")
                {
                    if (!_missedPongCounter.ContainsKey(client))
                        Console.WriteLine($"Not expecting a pong from {client.Client.RemoteEndPoint} but still got one.");
                    else
                        _missedPongCounter[client] -= 1;
                }
            }
            _statusMessageQueue.Clear();
        }

        private void _checkForClientLoad(TcpClient m, ClientType clientType)
        {
            int messageLength = m.Available;
            if (messageLength > 0)
            {
                byte[] msgBuffer = new byte[messageLength];
                m.GetStream().Read(msgBuffer, 0, msgBuffer.Length); // BLOCKS!!!

                string msg = Encoding.UTF8.GetString(msgBuffer);

                if (msg.StartsWith("STATUS:"))
                {
                    _statusMessageQueue.Enqueue((m, msg.Substring(msg.IndexOf(':') + 1)));
                }
                else if (clientType == ClientType.Messenger)
                {
                    string chatMsg = String.Format("{0}: {1}", _names[m], msg);
                    _messageQueue.Enqueue(chatMsg);
                }
            }
        }

        private void _checkForDisconnects()
        {
            foreach (TcpClient v in _viewers.ToArray())
            {
                if (Common.isDisconnected(v) || _clientsSaidBye.Contains(v) || (_missedPongCounter.TryGetValue(v, out int pongAmount) && pongAmount > maxMissedPongs))
                {
                    Console.WriteLine("Viewer {0} has left.", v.Client.RemoteEndPoint);

                    _viewers.Remove(v);
                    _cleanupClient(v);
                }
            }

            foreach (TcpClient m in _messengers.ToArray())
            {
                if (Common.isDisconnected(m) || _clientsSaidBye.Contains(m) || (_missedPongCounter.TryGetValue(m, out int pongAmount) && pongAmount > maxMissedPongs))
                {
                    string name = _names[m];

                    Console.WriteLine("Messenger {0} has left.", name);
                    _messageQueue.Enqueue(String.Format("{0} has disconnected", name));

                    _messengers.Remove(m);
                    _names.Remove(m);
                    _cleanupClient(m);
                }
            }
        }

        private void _handleNewConnection()
        {
            bool good = false;
            TcpClient newClient = _listener.AcceptTcpClient(); // Blocks!!
            NetworkStream netStream = newClient.GetStream();

            newClient.SendBufferSize = BufferSize;
            newClient.ReceiveBufferSize = BufferSize;

            EndPoint endPoint = newClient.Client.RemoteEndPoint;
            Console.WriteLine("Handling a new client from {0}...", endPoint);

            byte[] msgBuffer = new byte[BufferSize];
            int bytesRead = netStream.Read(msgBuffer, 0, msgBuffer.Length);  // BLOCKS@!!!!

            if (bytesRead > 0)
            {
                string msg = Encoding.UTF8.GetString(msgBuffer, 0, bytesRead);

                if (msg == "viewer")
                {
                    good = true;
                    _viewers.Add(newClient);

                    Console.WriteLine("{0} is a Viewer.", endPoint);

                    msg = String.Format("Welcome to the \"{0}\" Chat Server!", ChatName);
                    msgBuffer = Encoding.UTF8.GetBytes(msg);
                    netStream.Write(msgBuffer, 0, msgBuffer.Length);  // BLOCKS!!!
                }
                else if (msg.StartsWith("name:"))
                {
                    string name = msg.Substring(msg.IndexOf(':') + 1);
                    if ((name != string.Empty) && (!_names.ContainsValue(name)))
                    {
                        good = true;
                        _names.Add(newClient, name);
                        _messengers.Add(newClient);

                        Console.WriteLine("{0} is a Messenger with the name {1}", endPoint, name);

                        _messageQueue.Enqueue(String.Format("{0} has joined the chat.", name));
                    }
                }
                else
                {
                    Console.WriteLine("Wasn't able to identify {0} as a Viewer or Messenger.", endPoint);
                    _cleanupClient(newClient);
                }
            }

            if (!good)
                newClient.Close();
        }
    }
}
