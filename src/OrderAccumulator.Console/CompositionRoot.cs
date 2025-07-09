using OrderAccumulator.Contracts.Interfaces;
using OrderAccumulator.Contracts;
using OrderAccumulator.Infrastructure.Fix;
using OrderAccumulator.Infrastructure.Services;
using QuickFix;

namespace OrderAccumulator.ConsoleApp
{
    public static class CompositionRoot
    {
        public static IFixServerBootstrapper ConfigureFixServer()
        {
            // Application Layer
            IOrderProcessor orderProcessor = new OrderProcessor();

            // Infrastructure Layer
            IApplication fixApp = new FixServer(orderProcessor);
            IFixServerBootstrapper bootstrapper = new FixServerBootstrapper("Fix/fix.cfg", fixApp);

            return bootstrapper;
        }
    }
}
