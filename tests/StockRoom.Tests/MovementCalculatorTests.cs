using StockRoom.Models;
using StockRoom.Services;

namespace StockRoom.Tests;

public class MovementCalculatorTests
{
    [Theory]
    [InlineData(MovementKind.Purchase, 10, 5, 15)]
    [InlineData(MovementKind.AdjustmentIn, 10, 3, 13)]
    [InlineData(MovementKind.Sale, 10, 4, 6)]
    [InlineData(MovementKind.AdjustmentOut, 10, 10, 0)]
    [InlineData(MovementKind.StockTake, 10, 27, 27)]
    [InlineData(MovementKind.StockTake, 10, 0, 0)]
    public void Applies_each_kind_correctly(MovementKind kind, int current, int qty, int expected)
    {
        var (newQty, error) = MovementCalculator.Apply(current, kind, qty);

        Assert.Null(error);
        Assert.Equal(expected, newQty);
    }

    [Theory]
    [InlineData(MovementKind.Sale)]
    [InlineData(MovementKind.AdjustmentOut)]
    public void Refuses_to_drive_stock_negative(MovementKind kind)
    {
        var (newQty, error) = MovementCalculator.Apply(currentQuantity: 3, kind, quantity: 4);

        Assert.Null(newQty);
        Assert.Contains("Insufficient stock", error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Refuses_non_positive_quantities(int qty)
    {
        var (newQty, error) = MovementCalculator.Apply(10, MovementKind.Purchase, qty);

        Assert.Null(newQty);
        Assert.NotNull(error);
    }
}
