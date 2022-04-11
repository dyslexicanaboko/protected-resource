using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtectedResource.Entity;
using ProtectedResource.Lib;
using ProtectedResource.Lib.DataAccess;
using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;
using System.Threading;
using System.Threading.Tasks;

namespace ProtectedResource.Service
{
    public class WorkerService : BackgroundService
    {
        private readonly ILogger<WorkerService> _logger;
        private readonly ILogger<TableManager<RudimentaryEntity>> _loggerRudimentaryEntity;

        private readonly IConfigurationService _config;
        private readonly ICachingService _cachingService;
        private readonly IQueryToClassRepository _repository;

        public WorkerService(
            ILogger<WorkerService> logger,
            ILogger<TableManager<RudimentaryEntity>> loggerRudimentaryEntity,
            IConfigurationService config,
            ICachingService cachingService,
            IQueryToClassRepository repository)
        {
            _logger = logger;

            _loggerRudimentaryEntity = loggerRudimentaryEntity;

            _config = config;

            _cachingService = cachingService;
            _cachingService.Initialize();

            _repository = repository;
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
            
            //This cannot be a singleton, Technically it is scoped because it should only be instantiated once per resource
            //This needs to be done differently later. Might need to be instantiated inside of table manager, but I'm not sure yet.
            var queue = new MessagingQueueService(_config);

            //This should run inside of a monitor so that if this crashes for any reason it can be brought back up and
            //the error is logged appropriately and doesn't kill the service necessarily -- Issue #4
            var tm = new TableManager<RudimentaryEntity>(
                _repository, 
                _cachingService, 
                queue, 
                _config,
                _loggerRudimentaryEntity);

            var entity = $"{nameof(RudimentaryEntity)}";

            var tq = new TableQuery
            {
                Schema = "dbo",
                Table = entity
            };

            tm.Initialize(tq, 10);
            tm.Start(entity + "Queue");

            await Task.CompletedTask;
        }
    }
}
