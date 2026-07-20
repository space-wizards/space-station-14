using Content.Shared.EntityShapes.Shapes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityShapes.Components;

/// <summary>
/// Spawns an entity shape on MapInit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShapeSpawnerComponent : Component
{
    [DataField(required: true)]
    public EntityShape Shape;

    [DataField(required: true)]
    public EntProtoId Spawn;

    /// <summary>
    /// If true, aligns center coordinates of a spawner to the nearest tile.
    /// Used for tile patterns to be more stable when the origin
    /// is located on the edge between 2 or more tiles.
    /// </summary>
    [DataField]
    public bool AlignCoords;
}
