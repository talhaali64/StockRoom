using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StockRoom.Data;
using StockRoom.Models;

namespace StockRoom.Services;

/// <summary>
/// CSV import/export with SKU as the natural key — import upserts, so a shop can round-trip
/// its existing Excel sheet (export → edit → import) without creating duplicates.
/// </summary>
public class ProductCsvService(AppDbContext db, StockService stock)
{
    public const string Header = "Sku,Barcode,Name,Category,CostPrice,SalePrice,ReorderLevel,QuantityOnHand";

    public record ImportSummary(int Created, int Updated, int Skipped, List<string> Errors);

    public async Task<string> ExportAsync(CancellationToken ct = default)
    {
        var products = await db.Products.OrderBy(p => p.Sku).ToListAsync(ct);
        var sb = new StringBuilder(Header + "\n");
        foreach (var p in products)
        {
            sb.AppendLine(string.Join(',',
                Csv.Escape(p.Sku), Csv.Escape(p.Barcode), Csv.Escape(p.Name), Csv.Escape(p.Category),
                p.CostPrice.ToString(CultureInfo.InvariantCulture),
                p.SalePrice.ToString(CultureInfo.InvariantCulture),
                p.ReorderLevel, p.QuantityOnHand));
        }
        return sb.ToString();
    }

    public async Task<ImportSummary> ImportAsync(string csvText, CancellationToken ct = default)
    {
        var rows = Csv.Parse(csvText);
        int created = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        // Skip a header row if present.
        var start = rows.Count > 0 && rows[0][0].Trim().Equals("Sku", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        for (var i = start; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length < 7)
            {
                skipped++;
                errors.Add($"Row {i + 1}: expected at least 7 columns, got {row.Length}.");
                continue;
            }

            var sku = row[0].Trim();
            if (sku.Length == 0)
            {
                skipped++;
                errors.Add($"Row {i + 1}: missing SKU.");
                continue;
            }

            if (!decimal.TryParse(row[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) ||
                !decimal.TryParse(row[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var sale) ||
                !int.TryParse(row[6], out var reorder))
            {
                skipped++;
                errors.Add($"Row {i + 1} ({sku}): invalid number in CostPrice/SalePrice/ReorderLevel.");
                continue;
            }

            var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == sku, ct);
            if (product is null)
            {
                product = new Product { Sku = sku };
                db.Products.Add(product);
                created++;
            }
            else
            {
                updated++;
            }

            product.Barcode = string.IsNullOrWhiteSpace(row[1]) ? null : row[1].Trim();
            product.Name = row[2].Trim();
            product.Category = row[3].Trim();
            product.CostPrice = cost;
            product.SalePrice = sale;
            product.ReorderLevel = reorder;
            await db.SaveChangesAsync(ct);

            // Optional 8th column: set the on-hand count via a stock-take movement,
            // so even imports leave an audit trail.
            if (row.Length >= 8 && int.TryParse(row[7], out var qty) && qty != product.QuantityOnHand)
            {
                await stock.ApplyAsync(product.Id, MovementKind.StockTake, qty,
                    reference: "CSV import", note: $"Imported count {qty}", ct: ct);
            }
        }

        return new ImportSummary(created, updated, skipped, errors);
    }
}
