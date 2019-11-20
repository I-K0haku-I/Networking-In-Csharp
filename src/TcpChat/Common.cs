using System;
using System.Net.Sockets;
using System.Text;

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

        public static void CleanupNetworkResources(TcpClient _client)
        {
            var _msgStream = _client.GetStream();
            byte[] msgBuffer = Encoding.UTF8.GetBytes("STATUS:BYE");
            _msgStream.Write(msgBuffer, 0, msgBuffer.Length); // BLOCKs!!!

            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

    }

    public class SimpleTimer
    {
        private long previousTick;
        private long timer;
        private long period;
        private Action tickDelegate;
        private bool isFirstTime = true;

        public SimpleTimer(Action _tickDelegate, long _period)
        {
            timer = 0;
            period = _period;
            tickDelegate = _tickDelegate;
        }

        public void Tick()
        {
            if (isFirstTime)
            {
                previousTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                isFirstTime = false;
            }
            long deltaTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - previousTick;
            previousTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            timer += deltaTime;
            if (timer > period)
            {
                timer -= period;
                tickDelegate();
            }
        }

    }

}