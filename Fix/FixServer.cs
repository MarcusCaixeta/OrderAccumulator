using QuickFix;
using QuickFix.Fields;

namespace OrderAccumulator.Fix
{
    public class FixServer : MessageCracker, IApplication
    {
        private readonly Dictionary<string, decimal> _exposureBySymbol = new();
        private const decimal EXPOSURE_LIMIT = 100_000_000;

        public void FromApp(Message message, SessionID sessionID)
        {
            Crack(message, sessionID); 
        }

        public void OnMessage(QuickFix.FIX44.NewOrderSingle order, SessionID sessionID)
        {
            var symbol = order.Symbol.Value;
            var quantity = order.OrderQty.Value;
            var price = order.Price.Value;
            var side = order.Side.Value;

            decimal orderValue = CalculateOrderValue(price, quantity);
            decimal currentExposure = GetCurrentExposure(symbol);
            decimal newExposure = CalculateNewExposure(currentExposure, orderValue, side);

            if (ExceedsLimit(newExposure))
            {
                SendExecutionReport(order, sessionID, ExecType.REJECTED, OrdStatus.REJECTED, 0, 0, 0);
                LogRejection(symbol, newExposure);
                return;
            }

            _exposureBySymbol[symbol] = newExposure;
            SendExecutionReport(order, sessionID, ExecType.NEW, OrdStatus.NEW, quantity, quantity, price);
            LogAcceptance(symbol, newExposure);
        }

        private decimal CalculateOrderValue(decimal price, decimal quantity)
            => price * quantity;

        private decimal GetCurrentExposure(string symbol)
            => _exposureBySymbol.TryGetValue(symbol, out var value) ? value : 0;

        private decimal CalculateNewExposure(decimal current, decimal value, char side)
            => side == Side.BUY ? current + value : current - value;

        private bool ExceedsLimit(decimal exposure)
            => Math.Abs(exposure) > EXPOSURE_LIMIT;

        private void SendExecutionReport(
            QuickFix.FIX44.NewOrderSingle order, SessionID sessionID, char execType, char ordStatus,
            decimal leavesQty, decimal cumQty, decimal avgPx)
        {
            var execReport = new QuickFix.FIX44.ExecutionReport(new OrderID(Guid.NewGuid().ToString()), new ExecID(Guid.NewGuid().ToString()), new ExecType(execType),
                new OrdStatus(ordStatus), order.Symbol, order.Side, new LeavesQty(leavesQty), new CumQty(cumQty), new AvgPx(avgPx)
            );

            Session.SendToTarget(execReport, sessionID);
        }

        private void LogRejection(string symbol, decimal exposure)
        {
            Console.WriteLine($"Order REJECTED: {symbol}, new exposure = {exposure:C2} exceeds the limit.");
        }

        private void LogAcceptance(string symbol, decimal exposure)
        {
            Console.WriteLine($" Order ACCEPTED: {symbol}, new exposure = {exposure:C2}");
        }

        public void FromAdmin(Message message, SessionID sessionID) { }
        public void ToAdmin(Message message, SessionID sessionID) { }
        public void ToApp(Message message, SessionID sessionID)
            => Console.WriteLine(" Sending: " + message);
        public void OnCreate(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID)
            => Console.WriteLine($" Logon: {sessionID}");
        public void OnLogout(SessionID sessionID)
            => Console.WriteLine($" Logout: {sessionID}");
    }

}
