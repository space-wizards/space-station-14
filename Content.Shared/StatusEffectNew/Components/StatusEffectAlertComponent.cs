using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Used in conjunction with <see cref="StatusEffectComponent"/> to display an alert when the status effect is present.
/// </summary>
[RegisterComponent, NetworkedComponent]
[EntityCategory("StatusEffects")]
public sealed partial class StatusEffectAlertComponent : Component
{
    /// <summary>
    /// Status effect indication for the player.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert;

    /// <summary>
    /// If the status effect has a set end time and this is true, a duration
    /// indicator will be displayed with the alert.
    /// </summary>
    [DataField]
    public bool ShowDuration = true;
}
