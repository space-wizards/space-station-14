using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Used to store grid poly data to be used for UIs.
/// </summary>
[NetworkedComponent]
public abstract class SharedNavMapComponent : Component
{

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
