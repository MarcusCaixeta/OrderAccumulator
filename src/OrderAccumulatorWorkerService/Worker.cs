using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using OrderAccumulator.ConsoleApp;
using OrderAccumulator.Contracts.Interfaces; // <- onde está o CompositionRoot

public class Worker : BackgroundService
{
    private IFixServerBootstrapper _bootstrapper;

    public Worker()
    {
        _bootstrapper = CompositionRoot.ConfigureFixServer();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bootstrapper.Start();
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _bootstrapper.Stop();
        return base.StopAsync(cancellationToken);
    }
}
