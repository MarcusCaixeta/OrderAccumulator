
namespace OrderAccumulator.Contracts.Results
{
    public record ExecutionResult(
     char ExecType,
     char OrdStatus,
     decimal LeavesQty,
     decimal CumQty,
     decimal AvgPx,
     string Symbol,
     decimal Exposure
 );
}
