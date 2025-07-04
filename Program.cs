using OrderAccumulator.Fix;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

class Program
{
    static void Main()
    {
        var settings = new SessionSettings("Fix/fix.cfg");

        var app = new FixServer();

        var storeFactory = new FileStoreFactory(settings);

        ILogFactory? logFactory = settings.Get().Has("Verbose") && settings.Get().GetBool("Verbose") ? new FileLogFactory(settings) : new NullLogFactory();

        var acceptor = new ThreadedSocketAcceptor(app, storeFactory, settings, logFactory);

        acceptor.Start();

        Console.WriteLine("Aguardando Orders. Console.ReadLine() ");

        Console.ReadLine();

        acceptor.Stop();
    }
}
