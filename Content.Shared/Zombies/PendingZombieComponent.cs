using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Zombies;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Zombies;

/// <summary>
/// Does increasing damage to the subject over time until they turn into a zombie.
/// They should also have a ZombieComponent.
/// </summary>
[RegisterComponent]
public sealed class PendingZombieComponent : Component
{
    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    [DataField("infectionStarted", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan InfectionStarted;

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
    /// How much the virus hurts you (base, scales rapidly). Is copied from ZombieSettings.
    /// </summary>
    [DataField("virusDamage"), ViewVariables(VVAccess.ReadWrite)] public DamageSpecifier VirusDamage = new()
    {
        DamageDict = new ()
        {
            { "Blunt", 0.8 },
            { "Toxin", 0.2 },
        }
    };
}
