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
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// After this amount of the crew become xenoborgs, the shuttle will be automatically called.
    /// </summary>
    [DataField]
    public float XenoborgShuttleCallPercentage = 0.7f;

    /// <summary>
    /// The most xenoborgs that existed at one point.
    /// </summary>
    [DataField]
    public int MaxNumberXenoborgs = 0;

    /// <summary>
    /// If the announcment of the death of the mothership core was sent
    /// </summary>
    [DataField]
    public bool MothershipCoreDeathAnnouncmentSent = false;

    /// <summary>
    /// If the emergency shuttle trigged by <see cref="XenoborgShuttleCallPercentage"> was already called.
    /// Will only call once. if a admin recalls it. it won't call again unless this is set to false by a admin
    /// </summary>
    [DataField]
    public bool XenoborgShuttleCalled = false;
}
