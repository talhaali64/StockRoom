using System.ComponentModel.DataAnnotations;

namespace StockRoom.Models;

public class Product
{
    public int Id { get; set; }

    /// <summary>Unique stock-keeping unit, the natural key used by CSV import/export.</summary>
    [Required, MaxLength(60)]
    public string Sku { get; set; } = "";

    /// <summary>Optional EAN/UPC; USB barcode scanners type this into a lookup field.</summary>
    [MaxLength(60)]
    public string? Barcode { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(100)]
    public string Category { get; set; } = "";

    [Range(0, 10_000_000)]
    public decimal CostPrice { get; set; }

    [Range(0, 10_000_000)]
    public decimal SalePrice { get; set; }

    /// <summary>Low-stock alert threshold: on hand ≤ this → it shows in the low-stock report.</summary>
    [Range(0, 1_000_000)]
    public int ReorderLevel { get; set; } = 5;

    /// <summary>Denormalized current stock; always written together with a movement row.</summary>
    public int QuantityOnHand { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<StockMovement> Movements { get; set; } = [];

    public bool IsLowStock => QuantityOnHand <= ReorderLevel;
}
