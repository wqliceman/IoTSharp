using Figgle;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace IoTSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(FiggleFonts.Doom.Render("IoTSharp"));
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWindowsServices()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}