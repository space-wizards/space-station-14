using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Adds a status effect when triggered.
/// If TargetUser is true the user will gain the effect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SetStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Status effect to be added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<StatusEffectComponent> Status;

    /// <summary>
    /// How long the status lasts. Permanent until removed if null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}
