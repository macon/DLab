using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BondiGeek.Logging;
using DLab.Chrome.MessagingHost;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;

[assembly: OwinStartup(typeof(Startup1))]

namespace DLab.Chrome.MessagingHost
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            app.Run(async context =>
            {
                context.Response.ContentType = "application/json";
                var result = JObject.Parse(@"{""result"": ""null""}");

                LogWriter.Instance.WriteToLog("received request");
                if (context.Request.Path.HasValue)
                {
                    LogWriter.Instance.WriteToLog(context.Request.Path.Value);
                }

                if (context.Request.Path.HasValue)
                {
                    if (context.Request.Method == "GET" && context.Request.Path.Value.Equals("/gettabinfo", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            result = await ChromeClient.GetTabInfo();
                        }
                        catch (Exception e)
                        {
                            LogWriter.Instance.WriteToLog(e.Message);                        
                            LogWriter.Instance.WriteToLog(e.StackTrace);
                            result = JObject.Parse(@"{""result"": ""internal error""}");
                        }
                    }
                    else if (context.Request.Method == "POST" && context.Request.Path.StartsWithSegments(new PathString("/set-tab")))
                    {
                        try
                        {
                            var parts = context.Request.Path.Value.Split(new []{"/"}, StringSplitOptions.RemoveEmptyEntries);
                            var id = parts[1];
                            ChromeClient.SetTab(id);
                            result = JObject.Parse(@"{""result"": ""done""}");
                        }
                        catch (Exception e)
                        {
                            LogWriter.Instance.WriteToLog(e.Message);
                            LogWriter.Instance.WriteToLog(e.StackTrace);
                            result = JObject.Parse(@"{""result"": ""internal error""}");
                        }
                    }
                }
                await context.Response.WriteAsync(result.ToString(Formatting.None));

//                return Task.FromResult(context.Response.WriteAsync("{\"message\": \"Hello, world.\"}"));
//                return context.Response.WriteAsync(result.ToString(Formatting.None));
            });
        }
    }
}
