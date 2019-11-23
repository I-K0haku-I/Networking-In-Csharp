using System;
// using NetworkingLearning.TcpChat;
using NetworkingLearning;

namespace NetworkingLearning.TesterApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(string.Join(" ", args));
            // string toExecute = "server";

            string socketType = args[0];
            string addressOrChatName = args[1];
            int port = Int32.Parse(args[2]);


            switch (socketType)
            {
                case "server":
                    DoServer(addressOrChatName, port);
                    break;
                case "viewer":
                    DoViewer(addressOrChatName, port);
                    break;
                case "messenger":
                    DoMessenger(addressOrChatName, port);
                    break;
                case "gameserver":
                    DoGameServer(addressOrChatName, port);
                    break;
                case "gameclient":
                    DoGameClient(addressOrChatName, port);
                    break;
            }
        }
        

        private static void DoServer(string chatName, int port)
        {
            var server = new TcpChat.Server(chatName, port);

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs args) => { server.Shutdown(); args.Cancel = true; };

            server.Run();
        }

        private static void DoViewer(string address, int port)
        {
            var viewer = new TcpChat.Viewer(address, port);

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs args) => { viewer.Disconnect(); args.Cancel = true; };

            viewer.Connect();
            viewer.ListenForMessagesLoop();
        }

        private static void DoMessenger(string address, int port)
        {
            Console.Write("Enter a name to use: ");
            string name = Console.ReadLine();

            var messenger = new TcpChat.Messenger(address, port, name);

            messenger.Connect();
            messenger.SendMessagesLoop();
        }

        private static void DoGameServer(string gameName, int port)
        {
            var server = new TcpGame.Server(gameName, port);
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs args) => { server?.Shutdown(); args.Cancel = true; };

            server.Run();
        }

        private static void DoGameClient(string hostName, int port)
        {
            var client = new TcpGame.Client(hostName, port);
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs args) => { client?.Disconnect(); args.Cancel = true; };
            client.Connect();
            client.Run();
        }
    }
}
