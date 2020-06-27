using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogRotator.WinService
{
    public class LogRotator : BackgroundService
    {
        private readonly ILogger _logger;
        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private Rotator rotator;

        public LogRotator(ILogger<LogRotator> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                this.rotator = Rotator.Parse(Path.Combine(AssemblyDir, Constants.ConfigFile));
                this.rotator.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start LogRotator service", ex);
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                    await Task.Delay(1000, stoppingToken);

                this.rotator.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to stop LogRotator service", ex);
            }
        }
    }
}
