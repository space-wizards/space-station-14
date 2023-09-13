using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Explosion;

/// <summary>
///     Component that is used to send explosion overlay/visual data to an abstract explosion entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExplosionVisualsComponent : Component
{
    public MapCoordinates Epicenter;
    public Dictionary<int, List<Vector2i>>? SpaceTiles;
    public Dictionary<EntityUid, Dictionary<int, List<Vector2i>>> Tiles = new();
    public List<float> Intensity = new();
    public string ExplosionType = string.Empty;
    public Matrix3 SpaceMatrix;
    public ushort SpaceTileSize;
}

[Serializable, NetSerializable]
public sealed class ExplosionVisualsState : ComponentState
{
    public MapCoordinates Epicenter;
    public Dictionary<int, List<Vector2i>>? SpaceTiles;
    public Dictionary<NetEntity, Dictionary<int, List<Vector2i>>> Tiles;
    public List<float> Intensity;
    public string ExplosionType = string.Empty;
    public Matrix3 SpaceMatrix;
    public ushort SpaceTileSize;

    public ExplosionVisualsState(
        MapCoordinates epicenter,
        string typeID,
        List<float> intensity,
        Dictionary<int, List<Vector2i>>? spaceTiles,
        Dictionary<NetEntity, Dictionary<int, List<Vector2i>>> tiles,
        Matrix3 spaceMatrix,
        ushort spaceTileSize)
    {
        Epicenter = epicenter;
        SpaceTiles = spaceTiles;
        Tiles = tiles;
        Intensity = intensity;
        ExplosionType = typeID;
        SpaceMatrix = spaceMatrix;
        SpaceTileSize = spaceTileSize;
    }
}

[Serializable, NetSerializable]
public enum ExplosionAppearanceData
{
    Progress, // iteration index tracker for explosions that are still expanding outwards,
}
