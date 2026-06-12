using Microsoft.EntityFrameworkCore;
using StockRoom.Data;
using StockRoom.Models;

namespace StockRoom.Services;

/// <summary>Applies stock movements atomically: ledger row + product running total in one save.</summary>
public class StockService(AppDbContext db)
{
    public record Result(StockMovement? Movement, string? Error)
    {
        public bool Success => Movement is not null && Error is null;
    }

    public async Task<Result> ApplyAsync(
        int productId, MovementKind kind, int quantity,
        string? reference = null, string? note = null, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([productId], ct);
        if (product is null)
            return new Result(null, null); // not found

        var (newQuantity, error) = MovementCalculator.Apply(product.QuantityOnHand, kind, quantity);
        if (error is not null)
            return new Result(null, error);

        product.QuantityOnHand = newQuantity!.Value;

        var movement = new StockMovement
        {
            ProductId = product.Id,
            Product = product,
            Kind = kind,
            Quantity = quantity,
            QuantityAfter = newQuantity.Value,
            Reference = reference,
            Note = note,
        };
        db.Movements.Add(movement);
        await db.SaveChangesAsync(ct);

        return new Result(movement, null);
    }

    public async Task<List<Product>> LowStockAsync(CancellationToken ct = default)
        => await db.Products
            .Where(p => p.IsActive && p.QuantityOnHand <= p.ReorderLevel)
            .OrderBy(p => p.QuantityOnHand - p.ReorderLevel)
            .ToListAsync(ct);
}
