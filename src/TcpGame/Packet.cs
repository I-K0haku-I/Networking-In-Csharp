using System;
using Newtonsoft.Json;

namespace TcpGame
{
    public class Packet
    {
        [JsonProperty]
        public string Command { get; set; }

        [JsonProperty]
        public string Message { get; set; }

        public Packet(string command = "", string message = "")
        {
            Command = command;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format(
                "[Packet:\n" +
                "  Command=`{0}`\n" +
                "  Message=`{1}`",
                Command, Message
            );
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Packet FromJson(string jsonData)
        {
            return JsonConvert.DeserializeObject<Packet>(jsonData);
        }
    }
}
