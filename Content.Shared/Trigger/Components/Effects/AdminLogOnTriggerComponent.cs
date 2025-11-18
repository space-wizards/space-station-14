using Content.Shared.Database;
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
public sealed partial class AdminLogOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The message displayed in the logs describing what specifically was done by this trigger.
    /// The entity and user will be included alongside this message.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Message;

    /// <summary>
    /// The trigger keys and their weights.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogType LogType = LogType.Trigger;

    /// <summary>
    /// The trigger keys and their weights.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogImpact LogImpact = LogImpact.Low;
}
