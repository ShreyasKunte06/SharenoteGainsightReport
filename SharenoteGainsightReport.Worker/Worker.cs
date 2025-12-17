using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using SharenoteGainsight.Services;

namespace SharenoteGainsight.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started.");

            using (var scope = _serviceProvider.CreateScope())
            {
                var exportService = scope.ServiceProvider.GetRequiredService<IStaffExportService>();

                await exportService.ExecuteAsync(stoppingToken);
            }

            _logger.LogInformation("Worker completed.");
        }
    }
}
