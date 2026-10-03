namespace Content.Server.Radiation.Components;

/// <summary>
/// Attached to grid entities to track tile radiation sources.
/// </summary>
[RegisterComponent]
public sealed partial class GridTileRadiationComponent : Component
{
    public readonly Dictionary<(Vector2i Tile, ushort SourceId), TileSourceData> Sources = [];
}

public struct TileSourceData(Vector2i tile, ushort sourceId, float intensity, float slope, float halfLife)
{
    public Vector2i Tile = tile;
    public ushort SourceId = sourceId;
    public float Intensity = intensity;
    public float Slope = slope;
    public float HalfLife = halfLife;
}
