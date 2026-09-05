using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Server.RoundEnd;

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
    /// What will happen if zombies get more than 80%
    /// </summary>
    [DataField]
    public RoundEndBehavior ZombieRoundEndBehavior = RoundEndBehavior.ShuttleCall;

    /// <summary>
    /// Shuttle timer for when shuttle is called
    /// </summary>
    [DataField]
    public TimeSpan ZombieEvacShuttleTime = TimeSpan.FromMinutes(5);
}
