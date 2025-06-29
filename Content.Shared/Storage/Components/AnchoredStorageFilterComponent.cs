using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Entities with this component will eject all items that match the whitelist / blacklist when anchored.
/// It also doesn't allow any items to be inserted that fit the whitelist / blacklist while anchored.
/// </summary>
/// <example>
/// If you have a smuggler stash that has a player inside of it, you want to eject the player before its anchored so they don't get stuck
/// </example>
[RegisterComponent, NetworkedComponent, Access(typeof(AnchoredStorageFilterSystem))]
public sealed partial class AnchoredStorageFilterComponent : Component
{
    /// <summary>
    /// If not null, entities that do not match this whitelist will be ejected.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If not null, entities that match this blacklist will be ejected..
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
