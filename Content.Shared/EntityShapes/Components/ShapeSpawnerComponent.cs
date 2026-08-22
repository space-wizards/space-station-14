using Content.Shared.EntityShapes.Shapes;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared.EntityShapes.Components;

/// <summary>
/// Spawns an entity shape on MapInit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShapeSpawnerComponent : Component
{
    /// <summary>
    /// The shape to use to spawn the entities.
    /// </summary>
    [DataField(required: true)]
    public EntityShape Shape;

    /// <summary>
    /// Table to spawn at each point of the shape.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Spawn;

    /// <summary>
    /// If true, aligns center coordinates of a spawner to the nearest tile.
    /// Used for tile patterns to be more stable when the origin
    /// is located on the edge between 2 or more tiles.
    /// </summary>
    [DataField]
    public bool AlignCoords;

    /// <summary>
    /// Sets whether to delete the entity with this component after the spawner is finished.
    /// </summary>
    [DataField]
    public bool DeleteSpawnerAfterSpawn = true;
}
