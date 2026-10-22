using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.TooShortForUI.Components;

/// <summary>
/// Blocks the use of machine UI on blacklisted or non-whitelisted machines if the entity is not:
/// 1) buckled into something, like a chair or a bed
/// 2) weightless
/// or 3) vaulted on a table.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TooShortForUIComponent : Component
{
    /// <summary>
    /// These entities will be allowed.
    /// If the blacklist is null, blocks all entities.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// These entities will *not* be allowed.
    /// If the blacklist is null, blocks all entities.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public LocId? PopupText = "too-short-for-ui-cant-use";
}
