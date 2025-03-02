using Content.Server.Storage.EntitySystems;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Storage.Components;

/// <summary>
/// Spawns items from an entity table when used in hand.
/// </summary>
[RegisterComponent, Access(typeof(SpawnTableOnUseSystem))]
public sealed partial class SpawnTableOnUseComponent : Component
{
    /// <summary>
    /// The entity table to select entities from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Should the entity be deleted after being used?
    /// If false, it will be dropped on the ground instead.
    /// </summary>
    [DataField]
    public bool DeleteOnUse = true;
}
