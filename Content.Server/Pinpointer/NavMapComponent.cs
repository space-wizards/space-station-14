using Content.Shared.Pinpointer;
using Robust.Shared.Timing;

namespace Content.Server.Pinpointer;

/// <summary>
/// Used to store grid poly data to be used for UIs.
/// </summary>
[RegisterComponent]
public sealed class NavMapComponent : SharedNavMapComponent
{
    [ViewVariables]
    public readonly Dictionary<Vector2i, NavMapChunk> Chunks = new();
}

public sealed class NavMapChunk
{
    public GameTick LastUpdate;
    public readonly Vector2i Origin;
    public readonly Dictionary<Vector2i, Vector2[]> TileData = new();

    public NavMapChunk(Vector2i origin)
    {
        Origin = origin;
    }
}
