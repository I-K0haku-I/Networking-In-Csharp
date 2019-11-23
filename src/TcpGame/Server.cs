using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace TcpGame
{
    public class Server
    {
        private TcpListener _listener;

        private List<TcpClient> _clients = new List<TcpClient>();
        private List<TcpClient> _waitingLobby = new List<TcpClient>();

        private Dictionary<TcpClient, IGame> _gameClientIsIn = new Dictionary<TcpClient, IGame>();
        private List<IGame> _games = new List<IGame>();
        private List<Thread> _gameThreads = new List<Thread>();
        private IGame _nextGame;

        public readonly string Name;
        public readonly int Port;
        public bool Running { get; private set; }

        public Server(string name, int port)
        {
            Name = name;
            Port = port;
            Running = false;

            _listener = new TcpListener(IPAddress.Any, Port);
        }

        public void Shutdown()
        {
            if (Running)
            {
                Running = false;
                Console.WriteLine("Shutting down the Game(s) Server...");
            }
        }

        public void Run()
        {
            Console.WriteLine("Starting the \"{0}\" Game(s) Server on port {1}.", Name, Port);
            Console.WriteLine("Press Ctrl-C to shutdown the server at any time.");

            _nextGame = new GuessMyNumberGame(this);

            _listener.Start();
            Running = true;
            List<Task> newConnectionTasks = new List<Task>();
            Console.WriteLine("Waiting for incomming connections...");

            while (Running)
            {
                if (_listener.Pending())
                    newConnectionTasks.Add(_handleNewConnection());

                if (_waitingLobby.Count >= _nextGame.RequiredPlayers)
                {
                    int numPlayers = 0;
                    while (numPlayers < _nextGame.RequiredPlayers)
                    {
                        TcpClient player = _waitingLobby[0];
                        _waitingLobby.RemoveAt(0);

                        if (_nextGame.AddPlayer(player))
                            numPlayers++;
                        else
                            _waitingLobby.Add(player);
                    }

                    Console.WriteLine("Starting a \"{0}\" game.", _nextGame.Name);
                    Thread gameThread = new Thread(new ThreadStart(_nextGame.Run));
                    gameThread.Start();
                    _games.Add(_nextGame);
                    _gameThreads.Add(gameThread);

                    _nextGame = new GuessMyNumberGame(this);
                }

                foreach (TcpClient client in _waitingLobby.ToArray())
                {
                    EndPoint endPoint = client.Client.RemoteEndPoint;
                    bool disconnected = false;

                    Packet p = ReceivePacket(client).GetAwaiter().GetResult();
                    disconnected = (p?.Command == "bye") || IsDisconnected(client);

                    if (disconnected)
                    {
                        HandleDisconnectedClient(client);
                        Console.WriteLine("Client {0} has disconnected from the Game(s) Server.", endPoint);
                    }
                }

                Thread.Sleep(10);
            } // while running

            Task.WaitAll(newConnectionTasks.ToArray(), 1000);

            foreach (Thread thread in _gameThreads)
                thread.Abort();

            Parallel.ForEach(_clients, (client) =>
            {
                DisconnectClient(client, "The Game(s) Server is being shutdown.");
            });

            _listener.Stop();

            Console.WriteLine("The server has been shut down.");
        }


        private async Task _handleNewConnection()
        {
            TcpClient newCLient = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("new connection from {0}.", newCLient.Client.RemoteEndPoint);

            _clients.Add(newCLient);
            _waitingLobby.Add(newCLient);

            string msg = String.Format("Welcome to the \"{0}\" Games Server.\n", Name);
            await SendPacket(newCLient, new Packet("message", msg));
        }

        public void DisconnectClient(TcpClient client, string message = "")
        {
            Console.WriteLine("Disconnecting the client from {0}.", client.Client.RemoteEndPoint);

            if (message == "")
                message = "Goodbye.";

            Task byePacket = SendPacket(client, new Packet("bye", message));

            try
            {
                _gameClientIsIn[client]?.DisconnectClient(client);
            }
            catch (KeyNotFoundException) { }

            // Supposed to give time for client to send packet back, not sure if that's a good idea, what if it takes longer anyway?
            Thread.Sleep(100);

            byePacket.GetAwaiter().GetResult();
            HandleDisconnectedClient(client);
        }

        public void HandleDisconnectedClient(TcpClient client)
        {
            _clients.Remove(client);
            _waitingLobby.Remove(client);
            _cleanupClient(client);
        }

        # region Packet Transmission Methods
        public async Task SendPacket(TcpClient client, Packet packet)
        {
            try
            {
                byte[] jsonBuffer = Encoding.UTF8.GetBytes(packet.ToJson());
                byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

                byte[] msgBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
                lengthBuffer.CopyTo(msgBuffer, 0);
                jsonBuffer.CopyTo(msgBuffer, lengthBuffer.Length);

                await client.GetStream().WriteAsync(msgBuffer, 0, msgBuffer.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("There was an issue sending a packet.");
                Console.WriteLine("Reason: {0}", e.Message);
            }
        }

        public async Task<Packet> ReceivePacket(TcpClient client)
        {
            Packet packet = null;
            try
            {
                if (client.Available == 0)
                    return null;
                
                NetworkStream msgStream = client.GetStream();

                byte[] lengthBuffer = new byte[2];
                await msgStream.ReadAsync(lengthBuffer, 0, 2);
                ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);

                byte[] jsonBuffer = new byte[packetByteSize];
                await msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);

                string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                packet = Packet.FromJson(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("There was an issue recieving a packet.");
                Console.WriteLine("Reason: {0}", e.Message);
            }

            return packet;
        }
        #endregion

        #region TcpClient Helper Methods
        public static bool IsDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException)
            {
                return true;
            }
        }

        private static void _cleanupClient(TcpClient client)
        {
            client.GetStream().Close();
            client.Close();
        }
        #endregion
    }
}