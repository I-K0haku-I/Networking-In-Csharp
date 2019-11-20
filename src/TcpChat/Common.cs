using System;
using System.Net.Sockets;

namespace NetworkingLearning.TcpChat
{
    public class Common
    {
        public static bool isDisconnected(TcpClient client)
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
    }
    
}