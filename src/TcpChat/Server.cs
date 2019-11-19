using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

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

        private void _cleanupClient(TcpClient v)
        {
            throw new NotImplementedException();
        }

        private void _sendMessages()
        {
            throw new NotImplementedException();
        }

        private void _checkForNewMessages()
        {
            throw new NotImplementedException();
        }

        private void _checkForDisconnects()
        {
            throw new NotImplementedException();
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

            // byte[] msgBuffer
        }
    }
}
