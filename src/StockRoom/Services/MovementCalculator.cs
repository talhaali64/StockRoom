using StockRoom.Models;

namespace StockRoom.Services;

/// <summary>Pure stock math — the single source of truth for how each movement kind changes stock.</summary>
public static class MovementCalculator
{
    /// <summary>
    /// Returns the new on-hand quantity, or an error when the movement is invalid
    /// (non-positive quantity, or selling/removing more than is on hand).
    /// </summary>
    public static (int? NewQuantity, string? Error) Apply(int currentQuantity, MovementKind kind, int quantity)
    {
        if (kind != MovementKind.StockTake && quantity <= 0)
            return (null, "Quantity must be positive.");
        if (kind == MovementKind.StockTake && quantity < 0)
            return (null, "Counted quantity cannot be negative.");

        var newQuantity = kind switch
        {
            MovementKind.Purchase => currentQuantity + quantity,
            MovementKind.AdjustmentIn => currentQuantity + quantity,
            MovementKind.Sale => currentQuantity - quantity,
            MovementKind.AdjustmentOut => currentQuantity - quantity,
            MovementKind.StockTake => quantity,
            _ => currentQuantity,
        };

        if (newQuantity < 0)
            return (null, $"Insufficient stock: {currentQuantity} on hand, tried to remove {quantity}.");

        return (newQuantity, null);
    }
}
