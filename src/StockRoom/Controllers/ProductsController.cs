using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockRoom.Data;
using StockRoom.Dtos;
using StockRoom.Models;
using StockRoom.Services;

namespace StockRoom.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ProductResponse>> GetAll(
        [FromQuery] string? search, [FromQuery] string? category, [FromQuery] bool lowStockOnly = false)
    {
        var query = db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search) || p.Barcode == search);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);
        if (lowStockOnly)
            query = query.Where(p => p.QuantityOnHand <= p.ReorderLevel);

        return (await query.OrderBy(p => p.Sku).Take(500).ToListAsync()).Select(ProductResponse.From);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Get(int id)
    {
        var product = await db.Products.FindAsync(id);
        return product is null ? NotFound() : ProductResponse.From(product);
    }

    /// <summary>Barcode-scanner lookup: scanners "type" the code, this resolves it in one call.</summary>
    [HttpGet("by-barcode/{code}")]
    public async Task<ActionResult<ProductResponse>> GetByBarcode(string code)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Barcode == code || p.Sku == code);
        return product is null ? NotFound() : ProductResponse.From(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        if (await db.Products.AnyAsync(p => p.Sku == request.Sku))
            return Conflict(new { error = $"SKU '{request.Sku}' already exists." });

        var product = new Product
        {
            Sku = request.Sku.Trim(),
            Barcode = request.Barcode?.Trim(),
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, ProductResponse.From(product));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
            return NotFound();
        if (await db.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != id))
            return Conflict(new { error = $"SKU '{request.Sku}' already exists." });

        product.Sku = request.Sku.Trim();
        product.Barcode = request.Barcode?.Trim();
        product.Name = request.Name.Trim();
        product.Category = request.Category.Trim();
        product.CostPrice = request.CostPrice;
        product.SalePrice = request.SalePrice;
        product.ReorderLevel = request.ReorderLevel;
        product.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return ProductResponse.From(product);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Apply a stock movement (purchase, sale, adjustment, stock-take).</summary>
    [HttpPost("{id:int}/movements")]
    public async Task<ActionResult<MovementResponse>> Move(int id, MovementRequest request, [FromServices] StockService stock)
    {
        var result = await stock.ApplyAsync(id, request.Kind, request.Quantity, request.Reference, request.Note);
        if (result.Movement is null && result.Error is null)
            return NotFound();
        if (!result.Success)
            return UnprocessableEntity(new { error = result.Error });

        return MovementResponse.From(result.Movement!);
    }

    /// <summary>Movement history (the audit ledger) for one product.</summary>
    [HttpGet("{id:int}/movements")]
    public async Task<ActionResult<IEnumerable<MovementResponse>>> History(int id, [FromQuery] int take = 100)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id))
            return NotFound();

        var movements = await db.Movements.Include(m => m.Product)
            .Where(m => m.ProductId == id)
            .OrderByDescending(m => m.Id)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync();

        return Ok(movements.Select(MovementResponse.From));
    }
}
