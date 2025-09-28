using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(XenoborgsRuleSystem))]
[AutoGenerateComponentPause]
public sealed partial class XenoborgsRuleComponent : Component
{
    /// <summary>
    /// When the round will next check for round end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? NextRoundEndCheck;

    /// <summary>
    /// The amount of time between each check for the end of the round.
    /// </summary>
    [DataField]
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// After this amount of the crew become xenoborgs, the shuttle will be automatically called.
    /// </summary>
    [DataField]
    public float XenoborgShuttleCallPercentage = 0.7f;

    public bool MothershipCoreDeathAnnouncmentSent = false;
}
