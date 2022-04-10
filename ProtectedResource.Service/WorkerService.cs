using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProtectedResource.Service
{
    public class WorkerService : BackgroundService
    {
        private readonly ILogger<WorkerService> _logger;

        public WorkerService(ILogger<WorkerService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("======= Protected Resource Service Started =======");

            stoppingToken.Register(() =>
            {
                _logger.LogInformation("======= Protected Resource Service Stopped =======");
            });

            //For now, I am going to have one instance running, but in the future I want to be able to configure
            //as many instances as needed from any source


            await Task.CompletedTask;
        }
    }
}
