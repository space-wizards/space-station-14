namespace Content.Shared.Database;

/// <summary>
/// The category of the automod pattern.
/// </summary>
public enum AutomodFilterType : byte
{
    PlainTextWords,
    FalsePositives,
    FalseNegatives,
    Regex,
}
