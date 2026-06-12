using StockRoom.Models;
using StockRoom.Services;

namespace StockRoom.Data;

/// <summary>Seeds a small electronics shop with history so reports have data on first run.</summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Products.Any())
            return;

        var products = new[]
        {
            new Product { Sku = "KB-MX-87", Barcode = "8901111000011", Name = "Mechanical keyboard 87-key", Category = "Peripherals", CostPrice = 38, SalePrice = 59.99m, ReorderLevel = 5 },
            new Product { Sku = "MS-PRO-2", Barcode = "8901111000028", Name = "Wireless mouse Pro 2", Category = "Peripherals", CostPrice = 14, SalePrice = 24.99m, ReorderLevel = 8 },
            new Product { Sku = "MON-27-4K", Barcode = "8901111000035", Name = "27\" 4K monitor", Category = "Displays", CostPrice = 210, SalePrice = 319, ReorderLevel = 3 },
            new Product { Sku = "USB-C-1M", Barcode = "8901111000042", Name = "USB-C cable 1m", Category = "Cables", CostPrice = 1.8m, SalePrice = 6.99m, ReorderLevel = 20 },
            new Product { Sku = "HUB-7P", Barcode = "8901111000059", Name = "7-port USB hub", Category = "Accessories", CostPrice = 9, SalePrice = 19.99m, ReorderLevel = 6 },
            new Product { Sku = "SSD-1TB", Barcode = "8901111000066", Name = "1TB NVMe SSD", Category = "Storage", CostPrice = 55, SalePrice = 89.99m, ReorderLevel = 4 },
        };
        db.Products.AddRange(products);
        db.SaveChanges();

        // Build believable history through the same engine the API uses.
        var seedMoves = new (string Sku, MovementKind Kind, int Qty, string? Reference)[]
        {
            ("KB-MX-87", MovementKind.Purchase, 20, "PO-1001"),
            ("KB-MX-87", MovementKind.Sale, 14, "INV-2201"),
            ("MS-PRO-2", MovementKind.Purchase, 40, "PO-1001"),
            ("MS-PRO-2", MovementKind.Sale, 25, "INV-2202"),
            ("MS-PRO-2", MovementKind.Sale, 9, "INV-2210"),   // ends at 6 ≤ reorder 8 → low stock
            ("MON-27-4K", MovementKind.Purchase, 6, "PO-1002"),
            ("MON-27-4K", MovementKind.Sale, 4, "INV-2205"),  // ends at 2 ≤ reorder 3 → low stock
            ("USB-C-1M", MovementKind.Purchase, 100, "PO-1003"),
            ("USB-C-1M", MovementKind.Sale, 37, "INV-2207"),
            ("HUB-7P", MovementKind.Purchase, 15, "PO-1003"),
            ("HUB-7P", MovementKind.AdjustmentOut, 2, null),
            ("SSD-1TB", MovementKind.Purchase, 12, "PO-1004"),
            ("SSD-1TB", MovementKind.Sale, 5, "INV-2208"),
        };

        foreach (var (sku, kind, qty, reference) in seedMoves)
        {
            var product = products.Single(p => p.Sku == sku);
            var (newQty, _) = MovementCalculator.Apply(product.QuantityOnHand, kind, qty);
            product.QuantityOnHand = newQty!.Value;
            db.Movements.Add(new StockMovement
            {
                ProductId = product.Id,
                Kind = kind,
                Quantity = qty,
                QuantityAfter = newQty.Value,
                Reference = reference,
            });
        }

        db.SaveChanges();
    }
}
