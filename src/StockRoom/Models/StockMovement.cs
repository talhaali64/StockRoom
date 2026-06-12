using System.ComponentModel.DataAnnotations;

namespace StockRoom.Models;

/// <summary>
/// Append-only stock ledger. The product's QuantityOnHand is the running total;
/// QuantityAfter snapshots it per movement so history is auditable.
/// </summary>
public class StockMovement
{
    public long Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public MovementKind Kind { get; set; }

    /// <summary>Always positive; direction comes from Kind. For StockTake it is the counted total.</summary>
    public int Quantity { get; set; }

    /// <summary>Stock level after applying this movement.</summary>
    public int QuantityAfter { get; set; }

    /// <summary>PO / invoice / receipt number.</summary>
    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum MovementKind
{
    /// <summary>Goods received — increases stock.</summary>
    Purchase = 0,

    /// <summary>Sold to a customer — decreases stock.</summary>
    Sale = 1,

    /// <summary>Manual correction upward (found stock, customer return).</summary>
    AdjustmentIn = 2,

    /// <summary>Manual correction downward (damage, loss, theft).</summary>
    AdjustmentOut = 3,

    /// <summary>Physical count: sets the absolute level; Quantity is the counted total.</summary>
    StockTake = 4,
}
