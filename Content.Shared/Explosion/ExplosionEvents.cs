using Content.Shared.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Explosion;

/// <summary>
///     Raised directed at an entity to determine its explosion resistance, probably right before it is about to be
///     damaged by one.
/// </summary>
public sealed class GetExplosionResistanceEvent : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    ///     Can be set to whatever, but currently is being additively increased by components & clothing. So think twice
    ///     before multiplying or directly setting this.
    /// </summary>
    public float Resistance = 0;

    public readonly string ExplotionPrototype;

    SlotFlags IInventoryRelayEvent.TargetSlots =>  ~SlotFlags.POCKET;

    public GetExplosionResistanceEvent(string id)
    {
        ExplotionPrototype = id;
    }
}

/// <summary>
///     An explosion event. Used for client side rendering.
/// </summary>
[Serializable, NetSerializable]
public sealed class ExplosionEvent : EntityEventArgs
{
    public MapCoordinates Epicenter;

    public Dictionary<int, List<Vector2i>>? SpaceTiles;
    public Dictionary<EntityUid, Dictionary<int, List<Vector2i>>> Tiles;

    public List<float> Intensity;

    public string TypeID;

    public Matrix3 SpaceMatrix;

    public int ExplosionId;

    public ushort SpaceTileSize;

    public ExplosionEvent(
        int explosionId,
        MapCoordinates epicenter,
        string typeID,
        List<float> intensity,
        Dictionary<int, List<Vector2i>>? spaceTiles,
        Dictionary<EntityUid, Dictionary<int, List<Vector2i>>> tiles,
        Matrix3 spaceMatrix,
        ushort spaceTileSize)
    {
        Epicenter = epicenter;
        SpaceTiles = spaceTiles;
        Tiles = tiles;
        Intensity = intensity;
        TypeID = typeID;
        SpaceMatrix = spaceMatrix;
        ExplosionId = explosionId;
        SpaceTileSize = spaceTileSize;
    }
}

/// <summary>
///     Update visual rendering of the explosion to correspond to the servers processing of it.
/// </summary>
[Serializable, NetSerializable]
public sealed class ExplosionOverlayUpdateEvent : EntityEventArgs
{
    public int Index;
    public int ExplosionId;

    public ExplosionOverlayUpdateEvent(int explosionId, int index)
    {
        Index = index;
        ExplosionId = explosionId;
    }
}
