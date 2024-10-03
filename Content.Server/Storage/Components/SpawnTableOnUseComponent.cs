using Content.Server.Storage.EntitySystems;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;

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
    /// A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
