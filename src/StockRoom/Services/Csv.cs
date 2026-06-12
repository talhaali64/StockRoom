using System.Text;

namespace StockRoom.Services;

/// <summary>
/// Minimal RFC-4180 CSV reader/writer (quoted fields, embedded commas/quotes/newlines) —
/// enough for product import/export without a library dependency.
/// </summary>
public static class Csv
{
    public static string Escape(string? field)
    {
        field ??= "";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }

    /// <summary>Parses CSV text into rows of fields. Skips fully-empty lines.</summary>
    public static List<string[]> Parse(string text)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    fields.Add(current.ToString());
                    current.Clear();
                    break;
                case '\r':
                    break; // handled with the following \n (or ignored)
                case '\n':
                    fields.Add(current.ToString());
                    current.Clear();
                    if (fields.Any(f => f.Length > 0))
                        rows.Add([.. fields]);
                    fields.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        // trailing row without newline
        fields.Add(current.ToString());
        if (fields.Any(f => f.Length > 0))
            rows.Add([.. fields]);

        return rows;
    }
}
