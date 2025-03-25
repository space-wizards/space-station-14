using Content.Shared.Maps;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tiles;

/// <summary>
/// Replaces floor tiles around this entity when it spawns
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ReplaceFloorOnSpawnSystem))]
public sealed partial class ReplaceFloorOnSpawnComponent : Component
{
    /// <summary>
    /// The floor tiles that will be replaced. If null, will replace all.
    /// </summary>
    [DataField]
    public List<ProtoId<ContentTileDefinition>>? ReplaceableTiles = new();

    /// <summary>
    /// The tiles that it will replace. Randomly picked from the list.
    /// </summary>
    [DataField]
    public List<ProtoId<ContentTileDefinition>> ReplacementTiles = new();

    /// <summary>
    /// Whether or not there has to be a tile in the location to be replaced.
    /// </summary>
    [DataField]
    public bool ReplaceSpace = true;

    /// <summary>
    /// List of offsets from the base tile, used to determine which tiles will be replaced.
    /// </summary>
    [DataField]
    public List<Vector2i> Offsets = new() { Vector2i.Up, Vector2i.Down, Vector2i.Left, Vector2i.Right, Vector2i.Zero };
}
