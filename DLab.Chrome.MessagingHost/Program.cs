using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BondiGeek.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = System.Xml.Formatting;

namespace DLab.Chrome.MessagingHost
{
    public class Program
    {
//        public static void Main(string[] args)
//        {
//            JObject data;
//            while ((data = Read()) != null)
//            {
//                var processed = ProcessMessage(data);
//                Write(processed);
//                if (processed == "exit")
//                {
//                    return;
//                }
//            }
//        }

        public static void Main(string[] args)
        {
            using (Microsoft.Owin.Hosting.WebApp.Start<Startup1>("http://localhost:9000"))
            {
                //                Console.WriteLine("Press [enter] to quit...");
                //                Console.ReadLine();

                    LogWriter.Instance.WriteToLog($"main thread is {Thread.CurrentThread.ManagedThreadId}");
                    while (true)
                    {
                        Task.Delay(500).Wait();
                    }

            }
        }

        public static string ProcessMessage(JObject data)
        {
            var message = data["text"].Value<string>();

            switch (message)
            {
                case "test":
                    return "testing!";
                case "exit":
                    return "exit";
                default:
                    return "echo: " + message;
            }
        }

        public static JObject Read()
        {
            var stdin = Console.OpenStandardInput();
            var length = 0;

            var lengthBytes = new byte[4];
            stdin.Read(lengthBytes, 0, 4);
            length = BitConverter.ToInt32(lengthBytes, 0);

            var buffer = new char[length];
            using (var reader = new StreamReader(stdin))
            {
                while (reader.Peek() >= 0)
                {
                    reader.Read(buffer, 0, buffer.Length);
                }
            }

            return JsonConvert.DeserializeObject<JObject>(new string(buffer));
        }

        public static void Write(JToken data)
        {
            var json = new JObject {["data"] = data};

            var bytes = Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None));

            var stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
            stdout.Write(bytes, 0, bytes.Length);
            stdout.Flush();
        }
    }
}
