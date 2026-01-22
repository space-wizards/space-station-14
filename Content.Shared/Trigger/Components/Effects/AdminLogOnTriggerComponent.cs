using Content.Shared.Database;
using Content.Shared.Random;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// This component creates an admin log when receiving a trigger.
/// <see cref="BaseXOnTriggerComponent.TargetUser"/> is ignored.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdminLogOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The message displayed in the logs describing what specifically was done by this trigger.
    /// This entity and the user will be included alongside the message.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Message = string.Empty;

    /// <summary>
    /// What type of action took place?
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogType LogType = LogType.Trigger;

    /// <summary>
    /// How important is this trigger?
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogImpact LogImpact = LogImpact.Low;
}
