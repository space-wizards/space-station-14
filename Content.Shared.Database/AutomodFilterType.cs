namespace Content.Shared.Database;

public enum AutomodFilterType : byte
{
    PlainTextWords,
    FalsePositives,
    FalseNegatives,
    Regex,
}
