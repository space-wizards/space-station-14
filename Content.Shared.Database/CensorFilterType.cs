namespace Content.Shared.Database;

public enum CensorFilterType : byte
{
    PlainTextWords,
    FalsePositives,
    FalseNegatives,
    Regex,
}
