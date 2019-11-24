using System;
using System.Net.Sockets;
using System.Threading;


namespace TcpGame
{
    public class GuessMyNumberGame : IGame
    {
        private Server _server;
        private Random _rng;
        private TcpClient _player;
        private bool _needToDisconnectClient = false;

        public GuessMyNumberGame(Server server)
        {
            _server = server;
            _rng = new Random();
        }

        public string Name { get { return "Guess My Number"; } }

        public int RequiredPlayers { get { return 1; } }

        public bool AddPlayer(TcpClient client)
        {
            if (_player == null)
            {
                _player = client;
                return true;
            }

            return false;
        }

        public void DisconnectClient(TcpClient client)
        {
            _needToDisconnectClient = (client == _player);
        }

        public void Run()
        {
            if (_player == null)
                return;


            Packet introPacket = new Packet("message",
                "Welcome player, I want you to guess my number.\n" +
                "It's somewhere between (and including) 1 and 100.\n");
            _server.SendPacket(_player, introPacket).GetAwaiter().GetResult();

            int theNumber = _rng.Next(1, 101);
            Console.WriteLine("Our number is: {0}", theNumber);

            bool running = true;
            bool correct = false;
            bool clientConnected = true;
            bool clientDisconnectedGracefully = false;

            while (running)
            {
                Packet inputPacket = new Packet("input", "Your guess: ");
                _server.SendPacket(_player, inputPacket).GetAwaiter().GetResult();

                Packet answerPacket = null;
                while (answerPacket == null)
                {
                    answerPacket = _server.ReceivePacket(_player).GetAwaiter().GetResult();
                    Thread.Sleep(10);
                }

                if (answerPacket.Command == "bye")
                {
                    _server.HandleDisconnectedClient(_player);
                    clientDisconnectedGracefully = true;
                }

                if (answerPacket.Command == "input")
                {
                    Packet responsePacket = new Packet("message");

                    int theirGuess;
                    if (int.TryParse(answerPacket.Message, out theirGuess))
                    {
                        if (theirGuess == theNumber)
                        {
                            correct = true;
                            responsePacket.Message = "Correct! You win!\n";
                        }
                        else if (theirGuess < theNumber)
                            responsePacket.Message = "Too low.\n";
                        else if (theirGuess > theNumber)
                            responsePacket.Message = "Too high.\n";
                    }
                    else
                        responsePacket.Message = "That wasn't a valid number, try again.\n";

                    _server.SendPacket(_player, responsePacket).GetAwaiter().GetResult();
                }

                Thread.Sleep(10);

                running &= !correct;

                if (!_needToDisconnectClient && !clientDisconnectedGracefully)
                    clientConnected &= !Server.IsDisconnected(_player);
                else
                    clientConnected = false;

                running &= clientConnected;
            }

            if (clientConnected)
                _server.DisconnectClient(_player, "Thanks for playing \"Guess My Number\"!");
            else
                Console.WriteLine("Client disconnected from game.");

            Console.WriteLine("Ending a \"{0}\" game.", Name);
        }
    }
}