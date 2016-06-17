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

namespace DLab.Chrome.MessagingHost
{
    public class ChromeClient
    {
        public static async Task<JObject> GetTabInfo()
        {
            var json = @"{""op"": ""get-tabinfo""}";

            Write(JObject.Parse(json));
            var result = await Read();
            return result;
        }

        public static async Task<JObject> Read2()
        {
            using (var stream = Console.OpenStandardInput())
            {
                LogWriter.Instance.WriteToLog($"stream.CanRead=={stream.CanRead}");
                LogWriter.Instance.WriteToLog($"stream.CanSeek=={stream.CanSeek}");
                LogWriter.Instance.WriteToLog($"stream.CanTimeout=={stream.CanTimeout}");
                LogWriter.Instance.WriteToLog($"stream.CanWrite=={stream.CanWrite}");
//                stream.CanRead
                var tryCount = 0;
//                while (stream.Length == 0)
//                {
//                    if (tryCount > 10) { return JObject.Parse(@"{""result"": ""timeout""}"); }
//                    await Task.Delay(10);
//                }

                var lengthBytes = new byte[4];

                var bytesRead = stream.Read(lengthBytes, 0, 4);
                LogWriter.Instance.WriteToLog($"read {bytesRead} bytes for length prefix");

                var length = BitConverter.ToInt32(lengthBytes, 0);
                LogWriter.Instance.WriteToLog($"(1) receiving message of {length} bytes");

                var buffer = new byte[length];
                bytesRead = stream.Read(buffer, 0, length);
                LogWriter.Instance.WriteToLog($"read {bytesRead} bytes for message");

                var jsonResp = BitConverter.ToString(buffer);
                var result = JsonConvert.DeserializeObject<JObject>(jsonResp);
                return result;
            }
        }

        public static async Task<JObject> Read()
        {
            var stdin = Console.OpenStandardInput();

            var lengthBytes = new byte[4];
            stdin.Read(lengthBytes, 0, 4);
            var length = BitConverter.ToInt32(lengthBytes, 0);
            LogWriter.Instance.WriteToLog($"(1) receiving message of {length} bytes");

            using (var reader = new StreamReader(stdin))
            {
                var buffer = new char[length];

//                while (reader.Peek() >= 0)
//                {
                    LogWriter.Instance.WriteToLog($"before read from stream");
                    var readCount = await reader.ReadAsync(buffer, 0, length);
                    LogWriter.Instance.WriteToLog($"read {readCount} chars from stream. Peek={reader.Peek()}");
//                }
                LogWriter.Instance.WriteToLog("msg follows");
                var msgString = new string(buffer);
                LogWriter.Instance.WriteToLog(msgString);
                var  result = JsonConvert.DeserializeObject<JObject>(new string(buffer));
//                var result = JObject.Parse(msgString);
                LogWriter.Instance.WriteToLog("after parse");
                return result;
            }
        }

        private const int MaxTryCount = 5;

        public static void Write(JToken data)
        {
            LogWriter.Instance.WriteToLog("sending request to chrome");
            LogWriter.Instance.WriteToLog(data.ToString());

            var json = new JObject { ["data"] = data };

            var bytes = Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None));

            var stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
            stdout.Write(bytes, 0, bytes.Length);
            stdout.Flush();
        }

        public static void SetTab(string id)
        {
            var json = $"{{\"op\": \"set-tab\", \"id\": \"{id}\"}}";

            Write(JObject.Parse(json));
        }
    }
}
