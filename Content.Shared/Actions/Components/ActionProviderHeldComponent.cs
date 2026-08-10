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
    /// <summary>
    /// Localization ID of the popup shown when the user is not holding the action provider.
    /// </summary>
    [DataField]
    public LocId? OnFailPopup;
}
