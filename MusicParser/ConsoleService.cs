using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
 
namespace MusicParser
{
    [ExcludeFromCodeCoverage]
    internal class ConsoleService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;

        public ConsoleService(
            IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        Console.WriteLine("hello...");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        _appLifetime.StopApplication();
                    }
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
