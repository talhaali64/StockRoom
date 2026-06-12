using System.ComponentModel.DataAnnotations;
using StockRoom.Models;

namespace StockRoom.Dtos;

public record ProductRequest(
    [Required, MaxLength(60)] string Sku,
    [Required, MaxLength(200)] string Name,
    [MaxLength(60)] string? Barcode = null,
    [MaxLength(100)] string Category = "",
    [Range(0, 10_000_000)] decimal CostPrice = 0,
    [Range(0, 10_000_000)] decimal SalePrice = 0,
    [Range(0, 1_000_000)] int ReorderLevel = 5,
    bool IsActive = true);

public record ProductResponse(
    int Id, string Sku, string? Barcode, string Name, string Category,
    decimal CostPrice, decimal SalePrice, int ReorderLevel, int QuantityOnHand,
    bool IsLowStock, bool IsActive, DateTime CreatedAtUtc)
{
    public static ProductResponse From(Product p) => new(
        p.Id, p.Sku, p.Barcode, p.Name, p.Category, p.CostPrice, p.SalePrice,
        p.ReorderLevel, p.QuantityOnHand, p.IsLowStock, p.IsActive, p.CreatedAtUtc);
}

public record MovementRequest(
    MovementKind Kind,
    [Range(0, 1_000_000)] int Quantity,
    [MaxLength(100)] string? Reference = null,
    [MaxLength(300)] string? Note = null);

public record MovementResponse(
    long Id, int ProductId, string Sku, string ProductName,
    MovementKind Kind, int Quantity, int QuantityAfter,
    string? Reference, string? Note, DateTime CreatedAtUtc)
{
    public static MovementResponse From(StockMovement m) => new(
        m.Id, m.ProductId, m.Product?.Sku ?? "", m.Product?.Name ?? "",
        m.Kind, m.Quantity, m.QuantityAfter, m.Reference, m.Note, m.CreatedAtUtc);
}

public record InventoryValueReport(
    int ProductCount,
    int TotalUnits,
    decimal TotalCostValue,
    decimal TotalSaleValue,
    List<InventoryValueReport.CategoryLine> ByCategory)
{
    public record CategoryLine(string Category, int Units, decimal CostValue, decimal SaleValue);
}
