using QuickFix;
using OrderAccumulator.Infrastructure.Fix;
using OrderAccumulator.Contracts.Interfaces;
using OrderAccumulator.Infrastructure.Services;


class Program
{
    static void Main()
    {
        var processor = new OrderProcessor();
        IApplication fixApp = new FixServer(processor); 
        IFixServerBootstrapper bootstrapper = new FixServerBootstrapper("Fix/fix.cfg", fixApp);

        bootstrapper.Start();

        Console.WriteLine("Aguardando Orders... Pressione ENTER para sair.");
        Console.ReadLine();

        bootstrapper.Stop();
    }
}

