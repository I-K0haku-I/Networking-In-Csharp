using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace NetworkingLearning.TcpChat
{
    public class Server
    {
        private readonly string ChatName;
        private readonly int Port;
        public readonly int BufferSize = 2 * 1024;

        public bool Running { get; private set; }

        private TcpListener _listener;
        private List<TcpClient> _viewers = new List<TcpClient>();
        private List<TcpClient> _messengers = new List<TcpClient>();
        private Dictionary<TcpClient, string> _names = new Dictionary<TcpClient, string>();
        private Queue<string> _messageQueue = new Queue<string>();

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

            _listener.Start();
            Running = true;

            while (Running)
            {
                if (_listener.Pending())
                    _handleNewConnection();

                _checkForDisconnects();
                _checkForNewMessages();
                _sendMessages();

                Thread.Sleep(10);
            }

            foreach (TcpClient v in _viewers)
                _cleanupClient(v);
            foreach (TcpClient v in _messengers)
                _cleanupClient(v);
            _listener.Stop();

            Console.WriteLine("Server successfully shut down");
        }

        private static void _cleanupClient(TcpClient client)
        {
            client.GetStream().Close();
            client.Close();
        }

        private void _sendMessages()
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
        }

        private void _checkForNewMessages()
        {
            foreach (TcpClient m in _messengers)
            {
                int messageLength = m.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    m.GetStream().Read(msgBuffer, 0, msgBuffer.Length); // BLOCKS!!!

                    string msg = String.Format("{0}: {1}", _names[m], Encoding.UTF8.GetString(msgBuffer));
                    _messageQueue.Enqueue(msg);
                }
            }
        }

        private void _checkForDisconnects()
        {
            foreach (TcpClient v in _viewers.ToArray())
            {
                if (Common.isDisconnected(v))
                {
                    Console.WriteLine("Viewer {0} has left.", v.Client.RemoteEndPoint);

                    _viewers.Remove(v);
                    _cleanupClient(v);
                }
            }

            foreach (TcpClient m in _messengers.ToArray())
            {
                if (Common.isDisconnected(m))
                {
                    string name = _names[m];

                    Console.WriteLine("Messenger {0} has left.", name);
                    _messageQueue.Enqueue(String.Format("{0} has left the chat", name));

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
