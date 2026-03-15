using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// An action that requires power cell charge from the entity holding the action.
/// Requires <see cref="ActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem))]
[EntityCategory("Actions")]
public sealed partial class ChargeCostActionComponent : Component
{
    /// <summary>
    /// Battery charge used to perform the action.
    /// </summary>
    [DataField(required: true)]
    public float Charge = 14.4f;

    /// <summary>
    /// Popup shown to the user when there isn't enough power to create an item.
    /// </summary>
    [DataField(required: true)]
    public LocId NoPowerPopup = string.Empty;
}
