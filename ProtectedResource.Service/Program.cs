using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtectedResource.Lib.DataAccess;
using ProtectedResource.Lib.Services;

namespace ProtectedResource.Service
{
    //https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/host-and-deploy/windows-service/samples/3.x/BackgroundWorkerServiceSample
    public class Program
    {
        static void Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);

            var host = hostBuilder.Build();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddSingleton<ICachingService, CachingService>();
                    services.AddScoped<IMessagingQueueService, MessagingQueueService>();
                    services.AddScoped<IQueryToClassRepository, QueryToClassRepository>();

                    services.AddHostedService<Worker>();
                });
    }
}
