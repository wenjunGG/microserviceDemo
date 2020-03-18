using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OcelotApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        //public static IHostBuilder CreateHostBuilder(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //        .ConfigureWebHostDefaults(webBuilder =>
        //        {
        //            webBuilder.UseStartup<Startup>();
        //        });


        //public static IWebHost BuildWebHost(string[] args) =>
        //  WebHost.CreateDefaultBuilder(args)
        //.UseStartup<Startup>()
        //.ConfigureAppConfiguration(conf =>
        //{
        //    conf.AddJsonFile("configuration.json", optional: false, reloadOnChange: true);
        //})
        //.Build();

        public static IWebHost BuildWebHost(string[] args) =>
          WebHost.CreateDefaultBuilder(args)
         .ConfigureAppConfiguration((hostingContext, builder) => {
             builder
             .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
             .AddJsonFile("configuration.json", optional: false, reloadOnChange: true);
         })
         .UseStartup<Startup>()
         .Build();
    }
}
