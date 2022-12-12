namespace Content.Server.Pinpointer;

/// <summary>
/// Used to store grid poly data to be used for UIs.
/// </summary>
[RegisterComponent]
public sealed class NavMapComponent : Component
{
    [ViewVariables]
    public Dictionary<Vector2i, NavMapChunk> Chunks = new();
}

public sealed class NavMapChunk
{
    public Dictionary<Vector2i, Vector2[]> TileData = new();
}
