using Content.Shared.Popups;
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
    /// <summary>
    /// Whitelist that an entity must pass to use the action.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// Localization ID of the popup shown when the user does not pass <see cref="Whitelist"/>.
    /// </summary>
    [DataField]
    public LocId? OnFailPopup;

    /// <summary>
    /// What type the fail popup should appear as.
    /// </summary>
    [DataField]
    public PopupType OnFailPopupType = PopupType.SmallCaution;
}
