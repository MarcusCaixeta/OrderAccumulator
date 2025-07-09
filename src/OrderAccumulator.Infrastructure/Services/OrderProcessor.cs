using OrderAccumulator.Contracts;

namespace OrderAccumulator.Infrastructure.Services
{
    public class OrderProcessor : IOrderProcessor
    {
        private readonly Dictionary<string, decimal> _exposureBySymbol = new();
        private const decimal ExposureLimit = 100_000_000;
        private static readonly HashSet<string> ValidSymbols = new() { "PETR4", "VALE3", "VIIA4" };

        public bool TryProcessOrder(string symbol, decimal quantity, decimal price, char side, out string message)
        {
            if (!IsValidSymbol(symbol, out message) || !IsValidSide(side, out message) || !IsValidQuantity(quantity, out message) ||
                !IsValidPrice(price, out message))
            {
                return false;
            }

            decimal orderValue = price * quantity;
            decimal currentExposure = _exposureBySymbol.TryGetValue(symbol.ToUpperInvariant(), out var existingExposure) ? existingExposure : 0;
            decimal newExposure = side == '1' ? currentExposure + orderValue : currentExposure - orderValue;

            if (Math.Abs(newExposure) > ExposureLimit)
            {
                message = $"REJECTED: {symbol} exposure = {currentExposure:C2}";
                return false;
            }

            _exposureBySymbol[symbol.ToUpperInvariant()] = newExposure;
            message = $"ACCEPTED: {symbol} new exposure = {newExposure:C2}";
            return true;
        }

        private bool IsValidSymbol(string symbol, out string message)
        {
            if (string.IsNullOrWhiteSpace(symbol) || !ValidSymbols.Contains(symbol.ToUpperInvariant()))
            {
                message = $"REJECTED: símbolo inválido '{symbol}'.";
                return false;
            }
            message = string.Empty;
            return true;
        }

        private bool IsValidSide(char side, out string message)
        {
            if (side != '1' && side != '2')
            {
                message = $"REJECTED: lado inválido '{side}'.";
                return false;
            }
            message = string.Empty;
            return true;
        }

        private bool IsValidQuantity(decimal quantity, out string message)
        {
            if (quantity <= 0 || quantity >= 100_000 || quantity % 1 != 0)
            {
                message = $"REJECTED: quantidade inválida '{quantity}'. Deve ser inteiro entre 1 e 99.999.";
                return false;
            }
            message = string.Empty;
            return true;
        }

        private bool IsValidPrice(decimal price, out string message)
        {
            if (price <= 0 || price >= 1000 || Math.Round(price, 2) != price)
            {
                message = $"REJECTED: preço inválido '{price}'. Deve ser > 0, < 1000 e múltiplo de 0.01.";
                return false;
            }
            message = string.Empty;
            return true;
        }
        public decimal GetExposure(string symbol)
        {
            return _exposureBySymbol.TryGetValue(symbol, out var value) ? value : 0m;
        }

    }

}
