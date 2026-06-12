using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockRoom.Data;
using StockRoom.Dtos;
using StockRoom.Services;

namespace StockRoom.Controllers;

[ApiController]
[Route("api")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    /// <summary>Products at or below their reorder level, most urgent first.</summary>
    [HttpGet("reports/low-stock")]
    public async Task<IEnumerable<ProductResponse>> LowStock([FromServices] StockService stock)
        => (await stock.LowStockAsync()).Select(ProductResponse.From);

    /// <summary>Stock valuation at cost and at sale price, totalled and per category.</summary>
    [HttpGet("reports/inventory-value")]
    public async Task<InventoryValueReport> InventoryValue()
    {
        var products = await db.Products.Where(p => p.IsActive).ToListAsync();

        var byCategory = products
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "(uncategorized)" : p.Category)
            .Select(g => new InventoryValueReport.CategoryLine(
                g.Key,
                g.Sum(p => p.QuantityOnHand),
                g.Sum(p => p.QuantityOnHand * p.CostPrice),
                g.Sum(p => p.QuantityOnHand * p.SalePrice)))
            .OrderByDescending(c => c.CostValue)
            .ToList();

        return new InventoryValueReport(
            products.Count,
            products.Sum(p => p.QuantityOnHand),
            byCategory.Sum(c => c.CostValue),
            byCategory.Sum(c => c.SaleValue),
            byCategory);
    }

    /// <summary>Recent movements across all products (the activity feed).</summary>
    [HttpGet("movements")]
    public async Task<IEnumerable<MovementResponse>> RecentMovements([FromQuery] int take = 50)
        => (await db.Movements.Include(m => m.Product)
                .OrderByDescending(m => m.Id)
                .Take(Math.Clamp(take, 1, 500))
                .ToListAsync())
            .Select(MovementResponse.From);

    [HttpGet("products/export.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportCsv([FromServices] ProductCsvService csv)
        => File(System.Text.Encoding.UTF8.GetBytes(await csv.ExportAsync()), "text/csv", "products.csv");

    /// <summary>Upserts products by SKU from CSV (same columns as the export).</summary>
    [HttpPost("products/import")]
    [Consumes("text/csv", "text/plain")]
    public async Task<ProductCsvService.ImportSummary> ImportCsv([FromServices] ProductCsvService csv)
    {
        using var reader = new StreamReader(Request.Body);
        var text = await reader.ReadToEndAsync();
        return await csv.ImportAsync(text);
    }
}
