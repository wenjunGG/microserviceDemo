using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestTools;

namespace WebApplicationDemo1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            string port = Tools.GetRandAvailablePort().ToString();
            var _newArgs=args.Concat(new string[] { port }).ToArray();

            var config = new ConfigurationBuilder()
              .AddCommandLine(_newArgs)
              .Build();
            String ip = config["ip"];


            return  Host.CreateDefaultBuilder(_newArgs)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                      .UseUrls($"http://{ip}:{port}");
                });
        }
    }
}
