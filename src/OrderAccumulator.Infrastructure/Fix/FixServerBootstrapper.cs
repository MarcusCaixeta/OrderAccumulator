using OrderAccumulator.Contracts;
using OrderAccumulator.Contracts.Interfaces;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix;

namespace OrderAccumulator.Infrastructure.Fix
{
    public class FixServerBootstrapper : IFixServerBootstrapper
    {
        private readonly IAcceptor _acceptor;

        public FixServerBootstrapper(string configPath, IApplication app)
        {
            var settings = new SessionSettings(configPath);
            var storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = settings.Get().Has("Verbose") && settings.Get().GetBool("Verbose")
                                    ? (ILogFactory)new FileLogFactory(settings)
                                    : new NullLogFactory();


            _acceptor = new ThreadedSocketAcceptor(app, storeFactory, settings, logFactory);
        }

        public void Start() => _acceptor.Start();
        public void Stop() => _acceptor.Stop();
    }
}
