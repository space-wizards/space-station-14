using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
///     Entities with this component will eject all items that match the whitelist / blacklist when anchored.
///     It also doesn't allow any items to be inserted that fit the whitelist / blacklist while ancored.
/// </summary>
/// <example>
///     If you have a smuggler stash that has a player inside of it, you want to eject the player before its anchored so they don't get stuck
/// </example>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnchoredStorageFilterComponent : Component
{
    /// <summary>
    ///     Whitelist for entities that should be ejected (If null, ignore)
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     Whitelist for entities that should be ejected (If null, ignore)
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
