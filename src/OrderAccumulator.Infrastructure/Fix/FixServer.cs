using OrderAccumulator.Contracts;
using QuickFix;
using QuickFix.Fields;

namespace OrderAccumulator.Infrastructure.Fix
{
    public class FixServer : MessageCracker, IApplication
    {
        private readonly IOrderProcessor _processor;

        public FixServer(IOrderProcessor processor)
        {
            _processor = processor;
        }

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

            if (_processor.TryProcessOrder(symbol, quantity, price, side, out var logMessage))
            {
                SendExecutionReport(order, sessionID, ExecType.NEW, OrdStatus.NEW, quantity, quantity, price);
            }
            else
            {
                SendExecutionReport(order, sessionID, ExecType.REJECTED, OrdStatus.REJECTED, 0, 0, 0);
            }

            Console.WriteLine(logMessage);
        }

        private void SendExecutionReport(QuickFix.FIX44.NewOrderSingle order, SessionID sessionID,
            char execType, char ordStatus, decimal leavesQty, decimal cumQty, decimal avgPx)
        {
            var report = new QuickFix.FIX44.ExecutionReport();

            // Campos obrigatórios
            report.Set(new OrderID(Guid.NewGuid().ToString()));
            report.Set(new ExecID(Guid.NewGuid().ToString()));
            report.Set(new ExecType(execType));
            report.Set(new OrdStatus(ordStatus));
            report.Set(new ClOrdID(order.ClOrdID.Value));
            report.Set(order.Symbol);
            report.Set(order.Side);
            report.Set(new LeavesQty(leavesQty));
            report.Set(new CumQty(cumQty));
            report.Set(new AvgPx(avgPx));



            Session.SendToTarget(report, sessionID);
        }

        public void FromAdmin(Message message, SessionID sessionID) { }
        public void ToAdmin(Message message, SessionID sessionID) { }
        public void ToApp(Message message, SessionID sessionID) => Console.WriteLine(" Sending: " + message);
        public void OnCreate(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID) => Console.WriteLine($" Logon: {sessionID}");
        public void OnLogout(SessionID sessionID) => Console.WriteLine($" Logout: {sessionID}");
    }
}
