using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
///     Spawns items when used in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpawnItemsOnUseComponent : Component
{
    /// <summary>
    ///     The list of entities to spawn, with amounts and orGroups.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> Items = new();

    /// <summary>
    ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    ///     How many uses before the item should delete itself.
    /// </summary>
    [DataField]
    public int Uses = 1;
}
