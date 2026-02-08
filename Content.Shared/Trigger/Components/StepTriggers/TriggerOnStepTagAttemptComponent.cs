using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// Checks that the tripper has required tags to trigger.
/// </summary>
/// <remarks>
/// Empty RequiredTags makes always continue step trigger attempt.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepTagAttemptComponent : BaseStepTriggerOnXComponent
{
    /// <summary>
    /// List of tags required for the tripper to trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>>? RequiredTags;
}
