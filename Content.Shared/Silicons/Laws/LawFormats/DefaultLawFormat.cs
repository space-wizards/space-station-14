namespace Content.Shared.Silicons.Laws.LawFormats;

/// <summary>
/// Prints the law string without applying any additional formatting.
/// </summary>
public sealed partial class DefaultLawFormat : LawFormat
{
    public override string ApplyFormat(string toFormat)
    {
        return toFormat;
    }
}
