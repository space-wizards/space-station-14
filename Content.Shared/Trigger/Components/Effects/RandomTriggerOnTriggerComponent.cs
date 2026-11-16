using Content.Shared.Random;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// When triggered this component will choose a key and send a new trigger.
/// Trigger is sent to user if <see cref="BaseXOnTriggerComponent.TargetUser"/> is true.
/// </summary>
/// <remarks>Does not support recursive loops where this component triggers itself. Use <see cref="RepeatingTriggerComponent"/> instead.</remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomTriggerOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The trigger keys and their weights.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<WeightedRandomPrototype> RandomKeyOut;
}
