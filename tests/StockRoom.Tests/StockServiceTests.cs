using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockRoom.Data;
using StockRoom.Models;
using StockRoom.Services;

namespace StockRoom.Tests;

/// <summary>Stock + CSV behaviour against a real (in-memory SQLite) database.</summary>
public sealed class StockServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly StockService _stock;
    private readonly ProductCsvService _csv;

    public StockServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _stock = new StockService(_db);
        _csv = new ProductCsvService(_db, _stock);
    }

    private Product SeedProduct(int onHand = 0)
    {
        var product = new Product { Sku = "SKU-1", Name = "Widget", QuantityOnHand = onHand };
        _db.Products.Add(product);
        _db.SaveChanges();
        return product;
    }

    [Fact]
    public async Task Movement_updates_running_total_and_writes_ledger_row()
    {
        var product = SeedProduct();

        await _stock.ApplyAsync(product.Id, MovementKind.Purchase, 10, "PO-1");
        var sale = await _stock.ApplyAsync(product.Id, MovementKind.Sale, 3, "INV-1");

        Assert.True(sale.Success);
        Assert.Equal(7, product.QuantityOnHand);
        Assert.Equal(7, sale.Movement!.QuantityAfter);
        Assert.Equal(2, await _db.Movements.CountAsync());
    }

    [Fact]
    public async Task Oversell_is_rejected_and_leaves_no_trace()
    {
        var product = SeedProduct(onHand: 2);

        var result = await _stock.ApplyAsync(product.Id, MovementKind.Sale, 5);

        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.Error);
        Assert.Equal(2, product.QuantityOnHand);
        Assert.Equal(0, await _db.Movements.CountAsync());
    }

    [Fact]
    public async Task Low_stock_report_orders_most_urgent_first()
    {
        _db.Products.AddRange(
            new Product { Sku = "A", Name = "A", ReorderLevel = 5, QuantityOnHand = 5 },  // at level
            new Product { Sku = "B", Name = "B", ReorderLevel = 5, QuantityOnHand = 0 },  // most urgent
            new Product { Sku = "C", Name = "C", ReorderLevel = 5, QuantityOnHand = 50 }); // fine
        await _db.SaveChangesAsync();

        var low = await _stock.LowStockAsync();

        Assert.Equal(2, low.Count);
        Assert.Equal("B", low[0].Sku);
    }

    [Fact]
    public async Task Csv_import_upserts_by_sku()
    {
        SeedProduct(); // SKU-1 exists

        var summary = await _csv.ImportAsync(
            ProductCsvService.Header + "\n" +
            "SKU-1,,Widget renamed,Tools,2.5,5,3,12\n" +
            "SKU-2,123456,New thing,Tools,1,2,5,4\n");

        Assert.Equal(1, summary.Created);
        Assert.Equal(1, summary.Updated);
        Assert.Empty(summary.Errors);

        var renamed = await _db.Products.SingleAsync(p => p.Sku == "SKU-1");
        Assert.Equal("Widget renamed", renamed.Name);
        Assert.Equal(12, renamed.QuantityOnHand); // count applied via StockTake movement
        Assert.Equal(MovementKind.StockTake, (await _db.Movements.FirstAsync(m => m.ProductId == renamed.Id)).Kind);
    }

    [Fact]
    public async Task Csv_import_reports_bad_rows_without_aborting()
    {
        var summary = await _csv.ImportAsync(
            "SKU-OK,,Fine,Cat,1,2,3,0\n" +
            ",,missing sku,Cat,1,2,3\n" +
            "SKU-BAD,,Bad numbers,Cat,xx,2,3\n");

        Assert.Equal(1, summary.Created);
        Assert.Equal(2, summary.Skipped);
        Assert.Equal(2, summary.Errors.Count);
    }

    [Fact]
    public async Task Export_then_import_round_trips()
    {
        SeedProduct(onHand: 9);

        var csvText = await _csv.ExportAsync();
        var summary = await _csv.ImportAsync(csvText);

        Assert.Equal(0, summary.Created);
        Assert.Equal(1, summary.Updated);
        Assert.Empty(summary.Errors);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
