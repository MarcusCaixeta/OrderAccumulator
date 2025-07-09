using OrderAccumulator.Contracts;
using OrderAccumulator.Infrastructure.Services;
using Xunit;

namespace OrderAccumulator.Tests.Services
{
    public class OrderProcessorTests
    {
        private readonly OrderProcessor _processor;

        public OrderProcessorTests()
        {
            _processor = new OrderProcessor();
        }

        [Fact]
        public void BuyOrder_WithinLimit_ShouldBeAccepted()
        {
            var result = _processor.TryProcessOrder("PETR4", 1000, 50.00m, '1', out var message);

            Assert.True(result);
            Assert.Contains("ACCEPTED", message);
        }

        [Fact]
        public void BuyOrder_ExceedsLimit_ShouldBeRejected()
        {
            var result = _processor.TryProcessOrder("VIIA4", 1_000_000, 200.00m, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
        }


        [Fact]
        public void Quantity_EqualTo100000_ShouldBeRejected()
        {
            bool result = _processor.TryProcessOrder("PETR4", 100000, 10, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
            Assert.Contains("quantidade inválida", message, StringComparison.OrdinalIgnoreCase);
        }


        [Fact]
        public void TryProcessOrder_BuyOrderExceedingLimit_ShouldBeRejected()
        {
            decimal quantity = 2_000_000;
            decimal price = 100;

            bool result = _processor.TryProcessOrder("VALE3", quantity, price, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
        }

        [Fact]
        public void TryProcessOrder_SellOrderExceedingNegativeLimit_ShouldBeRejected()
        {
            decimal quantity = 1_100_000;
            decimal price = 100;

            bool result = _processor.TryProcessOrder("VALE3", quantity, price, '2', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
        }

        [Fact]
        public void TryProcessOrder_AlternatingBuys_ShouldAccumulateExposureCorrectly()
        {
            var symbol = "VALE3";
            var price = 999m;

            // 30.000 x 999 = 29.970.000
            var r1 = _processor.TryProcessOrder(symbol, 30000, price, '1', out var msg1);
            Assert.True(r1);
            Assert.Contains("ACCEPTED", msg1);

            // 40.000 x 999 = 39.960.000 → total = 69.930.000
            var r2 = _processor.TryProcessOrder(symbol, 40000, price, '1', out var msg2);
            Assert.True(r2);
            Assert.Contains("ACCEPTED", msg2);

            // 30.010 x 999 = 29.970.990 → total = 99.900.990
            var r3 = _processor.TryProcessOrder(symbol, 30010, price, '1', out var msg3);
            Assert.True(r3);
            Assert.Contains("ACCEPTED", msg3);
        }


        [Fact]
        public void TryProcessOrder_AlternatingBuys_ExceedLimitOnLast_ShouldBeRejected()
        {
            _processor.TryProcessOrder("VALE3", 500_000, 100, '1', out _);
            _processor.TryProcessOrder("VALE3", 400_000, 100, '1', out _);

            bool result = _processor.TryProcessOrder("VALE3", 200_000, 100, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
        }

        [Fact]
        public void MultipleBuyOrders_ShouldRespectExposureLimit()
        {
            var symbol = "VALE3";
            var price = 999m; // válido: < 1000 e múltiplo de 0.01
            var quantity1 = 50000; // 999 * 50.000 = 49.950.000
            var quantity2 = 40000; // 999 * 40.000 = 39.960.000 (total: 89.910.000)
            var quantity3 = 9000;  // 999 * 9.000 = 8.991.000 (total: 98.901.000)
            var quantity4 = 2000;  // 999 * 2.000 = 1.998.000 (total: 100.899.000 -> deve ser rejeitado)

            // Primeira ordem
            var result1 = _processor.TryProcessOrder(symbol, quantity1, price, '1', out var msg1);
            Assert.True(result1);
            Assert.Contains("ACCEPTED", msg1);

            // Segunda ordem
            var result2 = _processor.TryProcessOrder(symbol, quantity2, price, '1', out var msg2);
            Assert.True(result2);
            Assert.Contains("ACCEPTED", msg2);

            // Terceira ordem
            var result3 = _processor.TryProcessOrder(symbol, quantity3, price, '1', out var msg3);
            Assert.True(result3);
            Assert.Contains("ACCEPTED", msg3);

            // Quarta ordem (deve ser rejeitada)
            var result4 = _processor.TryProcessOrder(symbol, quantity4, price, '1', out var msg4);
            Assert.False(result4);
            Assert.Contains("REJECTED", msg4);
        }

        [Fact]
        public void RejectedOrder_ShouldNotAffectExposure()
        {
            var symbol = "VALE3";
            var price = 999m; // válido
                              // Ordem 1: 50.000 x 999 = 49.950.000
            _processor.TryProcessOrder(symbol, 50000, price, '1', out _);

            // Ordem 2: 40.000 x 999 = 39.960.000
            _processor.TryProcessOrder(symbol, 40000, price, '1', out _);

            // Ordem 3: 9.000 x 999 = 8.991.000
            _processor.TryProcessOrder(symbol, 9000, price, '1', out _);

            // Total até aqui: 98.901.000

            // Ordem 4: 2.000 x 999 = 1.998.000 → ultrapassa 100mi → deve ser rejeitada
            var resultRejected = _processor.TryProcessOrder(symbol, 2000, price, '1', out var msgRejected);
            Assert.False(resultRejected);
            Assert.Contains("REJECTED", msgRejected);

            // Ordem 5: 1.100 x 999 = 1.098.900 → deve ser aceita (total = exatamente 100.000.000)
            var resultAccepted = _processor.TryProcessOrder(symbol, 1100, price, '1', out var msgAccepted);
            Assert.True(resultAccepted);
            Assert.Contains("ACCEPTED", msgAccepted);
        }


        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("INVALIDO")]
        [InlineData("BTC")]
        public void InvalidSymbol_ShouldBeRejected(string symbol)
        {
            var result = _processor.TryProcessOrder(symbol, 1000, 50, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
            Assert.Contains("símbolo inválido", message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(1000)]
        [InlineData(10.005)] // não é múltiplo de 0.01
        public void InvalidPrice_ShouldBeRejected(decimal price)
        {
            var result = _processor.TryProcessOrder("PETR4", 1000, price, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
            Assert.Contains("preço inválido", message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(100000)]
        public void InvalidQuantity_ShouldBeRejected(decimal quantity)
        {
            var result = _processor.TryProcessOrder("PETR4", quantity, 10, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
            Assert.Contains("quantidade inválida", message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData('0')]
        [InlineData('X')]
        [InlineData('3')]
        public void InvalidSide_ShouldBeRejected(char side)
        {
            var result = _processor.TryProcessOrder("PETR4", 1000, 10, side, out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
            Assert.Contains("lado inválido", message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SellOrder_DeveReduzirExposicao()
        {
            _processor.TryProcessOrder("VALE3", 1000, 50, '1', out _); // +50.000

            var result = _processor.TryProcessOrder("VALE3", 500, 50, '2', out var message); // -25.000
            Assert.True(result);
            Assert.Contains("ACCEPTED", message);
        }
        [Fact]
        public void VendaSemCompra_DeveSerRejeitadaAoUltrapassarLimite()
        {
            var result = _processor.TryProcessOrder("VALE3", 1_100_000, 100, '2', out var message); // -110M
            Assert.False(result);
            Assert.Contains("REJECTED", message);
        }
        [Fact]
        public void VendaDepoisCompra_DeveReequilibrarExposicao()
        {
            _processor.TryProcessOrder("VALE3", 50, 999m, '2', out _);

            var result = _processor.TryProcessOrder("VALE3", 60, 999m, '1', out var message);

            Assert.True(result);
            Assert.Contains("ACCEPTED", message);
        }

        [Theory]
        [InlineData(0.011)]
        [InlineData(99.999)]
        [InlineData(100.001)]
        public void PrecoComMuitasCasasNaoMultiploDe001_DeveSerRejeitado(decimal preco)
        {
            var result = _processor.TryProcessOrder("PETR4", 1000, preco, '1', out var message);

            Assert.False(result);
            Assert.Contains("REJECTED", message);
            Assert.Contains("preço inválido", message, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public void OrdemQueLevaExposicaoAExatamente100Milhoes_DeveSerAceita()
        {
            var price = 100m;
            var quantity1 = 999_000; // 999000 * 100 = 99.900.000
            var quantity2 = 1000;    // 1000 * 100 = 100.000 → total = 100.000.000

            _processor.TryProcessOrder("PETR4", quantity1, price, '1', out _);
            var result = _processor.TryProcessOrder("PETR4", quantity2, price, '1', out var message);

            Assert.True(result);
            Assert.Contains("ACCEPTED", message);
        }


    }
}
