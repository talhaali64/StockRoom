using StockRoom.Services;

namespace StockRoom.Tests;

public class CsvTests
{
    [Fact]
    public void Parses_simple_rows()
    {
        var rows = Csv.Parse("a,b,c\n1,2,3");

        Assert.Equal(2, rows.Count);
        Assert.Equal(["a", "b", "c"], rows[0]);
        Assert.Equal(["1", "2", "3"], rows[1]);
    }

    [Fact]
    public void Handles_quoted_fields_with_commas_and_quotes()
    {
        var rows = Csv.Parse("""SKU-1,"27"" monitor, 4K",Displays""");

        Assert.Single(rows);
        Assert.Equal("27\" monitor, 4K", rows[0][1]);
    }

    [Fact]
    public void Handles_newlines_inside_quotes_and_crlf()
    {
        var rows = Csv.Parse("a,\"line1\nline2\",c\r\nd,e,f\r\n");

        Assert.Equal(2, rows.Count);
        Assert.Equal("line1\nline2", rows[0][1]);
        Assert.Equal(["d", "e", "f"], rows[1]);
    }

    [Fact]
    public void Skips_blank_lines()
    {
        var rows = Csv.Parse("a,b\n\n\nc,d\n");
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Escape_round_trips_through_parse()
    {
        var nasty = "He said \"hi\", then\nleft";
        var line = $"{Csv.Escape("SKU-1")},{Csv.Escape(nasty)}";

        var rows = Csv.Parse(line);

        Assert.Equal(nasty, rows[0][1]);
    }
}
