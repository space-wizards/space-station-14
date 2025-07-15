using Content.Shared.Timing;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will activate an UseDelay on the target when triggered.
/// </summary>
/// <remarks>
/// TODO: Support specific UseDelay IDs for each trigger key.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UseDelayTriggerConditionComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The UseDelay Id to delay.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string UseDelayId = UseDelaySystem.DefaultId;
}
