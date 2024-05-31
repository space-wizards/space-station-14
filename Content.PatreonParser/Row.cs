using CsvHelper.Configuration.Attributes;

namespace Content.PatreonParser;

// These need to be properties or CSVHelper will not find them
public sealed class Row
{
    [Name("Id"), Index(0)]
    public int Id { get; set; }

    [Name("Trigger"), Index(1)]
    public string Trigger { get; set; } = default!;

    [Name("Time"), Index(2)]
    public DateTime Time { get; set; }

    [Name("Content"), Index(3)]
    public string ContentJson { get; set; } = default!;
}
