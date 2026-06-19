using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.CombatMode.AttackWhitelist;

/// <summary>
/// A entity with this component can only attack entities that pass the white/blacklist from this component
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AttackWhitelistComponent : Component
{
    /// <summary>
    /// Popup message that is displayed when the entity fails to attack a entity because of this component
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? FailedMessage;

    /// <summary>
    /// whitelist of entities that can be attack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// whitelist of entities that can't be attack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
