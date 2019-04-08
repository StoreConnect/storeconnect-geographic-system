using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace venueindexer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configApp) => 
                {
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient<IESHttpClient, ESHttpClient>();
                    services.AddHostedService<VenueIndexerService>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

    }
}
