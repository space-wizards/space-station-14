using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Prevents an action from being used by entities that do not pass the configured whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionRestrictionsSystem))]
[EntityCategory("Actions")]
public sealed partial class ActionUserWhitelistComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public LocId? Popup;
}
