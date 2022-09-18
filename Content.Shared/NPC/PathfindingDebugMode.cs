namespace Content.Shared.NPC;

[Flags]
public enum PathfindingDebugMode : byte
{
    None = 0,
    Breadcrumbs = 1 << 0,
    Chunks = 1 << 1,
}
