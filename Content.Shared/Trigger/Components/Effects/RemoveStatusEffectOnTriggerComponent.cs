using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Removes a status effect when triggered.
/// If TargetUser is true the user loses the status.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Status effect to be removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<StatusEffectComponent> Status;
}
