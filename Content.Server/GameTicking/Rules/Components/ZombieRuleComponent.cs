using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed partial class ZombieRuleComponent : Component
{
    /// <summary>
    /// When the round will next check for round end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextRoundEndCheck;

    /// <summary>
    /// The amount of time between each check for the end of the round.
    /// </summary>
    [DataField]
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// After this amount of the crew become zombies, the shuttle will be automatically called.
    /// </summary>
    [DataField]
    public float ZombieShuttleCallPercentage = 0.7f;

    /// <summary>
    /// Is the automatical shuttle called.
    /// </summary>
    public bool ZombieShuttleCalled = false;

    /// <summary>
    /// The reason when shuttle called.
    /// </summary>
    [DataField]
    public string ShuttleCallReason = "zombie-shuttle-call";

    /// <summary>
    /// The reason why shuttle can be recalled after certain zombie percentage.
    /// </summary>
    [DataField]
    public string ShuttleCallUnavailableReason = "zombie-shuttle-unavailable";
}
