using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Generic component used to track a gamerule that's start has been delayed.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class DelayedStartRuleComponent : Component
{
    /// <summary>
    /// The time at which the rule will start properly.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RuleStartTime;
}
