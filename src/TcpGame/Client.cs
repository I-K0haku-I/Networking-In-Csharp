using System;
using System.Text;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace TcpGame
{
    public class Client
    {
        private TcpClient _client;
        private NetworkStream _msgStream;
        private Dictionary<string, Func<string, Task>> _commandHandlers = new Dictionary<string, Func<string, Task>>();
        private readonly string ServerAddress;
        private readonly int Port;

        public bool Running { get; private set; }

        private bool _clientRequestedDisconnect = false;

        public Client(string serverAddress, int port)
        {
            _client = new TcpClient();
            Running = false;

            ServerAddress = serverAddress;
            Port = port;
        }

        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        public void Connect()
        {
            try
            {
                _client.Connect(ServerAddress, Port);
            }
            catch (SocketException se)
            {
                Console.WriteLine("[ERROR] {0}", se.Message);
            }

            if (_client.Connected)
            {
                Console.WriteLine("Connected to the server at {0}.", _client.Client.RemoteEndPoint);
                Running = true;

                _msgStream = _client.GetStream();

                _commandHandlers["bye"] = _handleBye;
                _commandHandlers["message"] = _handleMessage;
                _commandHandlers["input"] = _handleInput;
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server {0}:{1}.", ServerAddress, Port);
            }
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnecting from the server...");
            Running = false;
            _clientRequestedDisconnect = true;
            _sendPacket(new Packet("bye")).GetAwaiter().GetResult();
        }

        public void Run()
        {
            bool wasRunning = Running;

            List<Task> tasks = new List<Task>();
            while (Running)
            {
                tasks.Add(_handleIncomingPackets());

                Thread.Sleep(10);

                if (_isDisconnected(_client) && !_clientRequestedDisconnect)
                {
                    Running = false;
                    Console.WriteLine("The server has disconnected from us ungracefully. \n :[");
                }
            }

            Task.WaitAll(tasks.ToArray(), 1000);

            _cleanupNetworkResources();
            if (wasRunning)
                Console.WriteLine("Disconnected.");
        }

        private async Task _sendPacket(Packet packet)
        {
            try
            {
                byte[] jsonBuffer = Encoding.UTF8.GetBytes(packet.ToJson());
                byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

                byte[] packetBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
                lengthBuffer.CopyTo(packetBuffer, 0);
                jsonBuffer.CopyTo(packetBuffer, lengthBuffer.Length);

                await _msgStream.WriteAsync(packetBuffer, 0, packetBuffer.Length);
            }
            catch (Exception) { }
        }

        private async Task _handleIncomingPackets()
        {
            try
            {
                if (_client.Available > 0)
                {
                    byte[] lengthBuffer = new byte[2];
                    await _msgStream.ReadAsync(lengthBuffer, 0, 2);
                    ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);

                    byte[] jsonBuffer = new byte[packetByteSize];
                    await _msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);

                    string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                    Packet packet = Packet.FromJson(jsonString);

                    try
                    {
                        await _commandHandlers[packet.Command](packet.Message);
                    }
                    catch (KeyNotFoundException) { }
                }
            }
            catch (Exception) { }
        }

        #region Command Handlers
        private Task _handleBye(string message)
        {
            Console.WriteLine("The server is disconnecting us with this message:");
            Console.WriteLine(message);

            Running = false;
            return Task.FromResult(0);
        }

        private Task _handleMessage(string message)
        {
            Console.Write(message);
            return Task.FromResult(0);
        }

        private async Task _handleInput(string message)
        {
            Console.Write(message);
            string responseMsg = Console.ReadLine();

            Packet resp = new Packet("input", responseMsg);
            await _sendPacket(resp);
        }
        #endregion

        #region TcpClien Helper Metrhods
        private static bool _isDisconnected(TcpClient client)
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
        #endregion
    }
}