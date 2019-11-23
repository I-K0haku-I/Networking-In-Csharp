using System.Net.Sockets;

namespace TcpGame
{
    interface IGame
    {
        #region Properties
        string Name { get; }

        int RequiredPlayers { get; }
        #endregion

        #region Functions

        bool AddPlayer(TcpClient player);
        void DisconnectClient(TcpClient client);

        void Run();
        #endregion
    }

}