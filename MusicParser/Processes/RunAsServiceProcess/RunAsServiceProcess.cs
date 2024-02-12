using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace musicParser.Processes
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private FileSystemWatcher watcher;
        private readonly string folderToProcess;
        private readonly bool generateLogOnOK;
        private Lifecycle lifecycle;

        public TimedHostedService()
        {
            this.folderToProcess = Program.Configuration.GetValue<string>("folderToProcess");
            this.generateLogOnOK = Program.Configuration.GetValue<bool>("generateLogOnOK");
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Service starting...");

            this.lifecycle = ActivatorUtilities.CreateInstance<Lifecycle>(Program.host.Services);

            watcher = new FileSystemWatcher(folderToProcess)
            {
                NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size
            };

            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;

            return Task.CompletedTask;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Created: {e.FullPath}");

            // Execute lifecycle for the notified created folder
            lifecycle.Execute(e.FullPath, generateLogOnOK);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Service stopping...");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing watcher...");
            watcher.Dispose();
        }
    }
}