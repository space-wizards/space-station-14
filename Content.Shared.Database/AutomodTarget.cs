namespace Content.Shared.Database;

/// <summary>
/// The text input that the automod filter applies to.
/// </summary>
[Flags]
public enum AutomodTarget
{
    None  = 0,
    IC    = 1 << 0,
    OOC   = 1 << 1,
    Emote = 1 << 2,
    Name  = 1 << 3,
}
