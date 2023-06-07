using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Zombies;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Zombies;

/// <summary>
/// Temporary because diseases suck.
/// </summary>
[RegisterComponent]
public sealed class PendingZombieComponent : Component
{
    [DataField("settings")] public ZombieSettings Settings = default!;
    /// <summary>
    /// Settings for any victims we might have (if they are not the same as our settings)
    /// </summary>
    [DataField("victimSettings"), ViewVariables(VVAccess.ReadOnly)]
    public ZombieSettings? VictimSettings;

    /// <summary>
    /// Our family (describes how we became a zombie and where the rules are)
    /// </summary>
    [DataField("family"), ViewVariables(VVAccess.ReadOnly)]
    public ZombieFamily Family = default!;

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// Scales damage over time.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("infectedSecs")]
    public int InfectedSecs;

    /// <summary>
    /// Number of seconds that a typical infection will last before the player is totally overwhelmed with damage and
    ///   dies.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxInfectionLength")]
    public float MaxInfectionLength = 120f;

    /// <summary>
    /// A minimum multiplier applied to Damage once you are in crit to get you dead and ready for your next life
    ///   as fast as possible.
    /// </summary>
    [DataField("minimumCritMultiplier")]
    public float MinimumCritMultiplier = 10;

    /// <summary>
    ///    When Initial Infected can first turn (copied from ZombieRuleComponent)
    /// </summary>
    [DataField("firstTurnAllowed"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan FirstTurnAllowed = TimeSpan.Zero;


    /// <summary>
    /// How much the virus hurts you (base, scales rapidly)
    /// </summary>
    public DamageSpecifier VirusDamage => Settings.VirusDamage;
}
