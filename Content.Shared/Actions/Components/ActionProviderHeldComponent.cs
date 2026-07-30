using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Prevents an action from being used unless the entity providing it is held by the user.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionRestrictionsSystem))]
[EntityCategory("Actions")]
public sealed partial class ActionProviderHeldComponent : Component
{
    [DataField]
    public LocId? Popup;
}
