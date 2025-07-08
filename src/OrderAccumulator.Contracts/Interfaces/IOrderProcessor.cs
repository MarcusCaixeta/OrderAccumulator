
namespace OrderAccumulator.Contracts
{
    public interface IOrderProcessor
    {
        bool TryProcessOrder(string symbol, decimal quantity, decimal price, char side, out string message);
    }
}


