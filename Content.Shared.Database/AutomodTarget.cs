namespace Content.Shared.Database;

[Flags]
public enum AutomodTarget
{
    None  = 0,
    IC    = 1 << 0,
    OOC   = 1 << 1,
    Emote = 1 << 2,
    Name  = 1 << 3,
}
