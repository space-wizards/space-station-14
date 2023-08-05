using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Zombies;

/// <summary>
/// Does increasing damage to the subject over time until they turn into a zombie.
/// They should also have a ZombieComponent.
/// </summary>
[RegisterComponent]
public sealed class PendingZombieComponent : Component
{
    /// <summary>
    /// The amount of time before the infected begins to take damage in seconds.
    /// </summary>
    [DataField("gracePeriod"), ViewVariables(VVAccess.ReadWrite)]
    public float GracePeriod = 0f;

    /// <summary>
    /// A multiplier for <see cref="Damage"/> applied when the entity is in critical condition.
    /// </summary>
    [DataField("critDamageMultiplier")]
    public float CritDamageMultiplier = 10f;

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    [DataField("infectionStarted", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan InfectionStarted;

    /// <summary>
    /// The chance each second that a warning will be shown.
    /// </summary>
    [DataField("infectionWarningChance")]
    public float InfectionWarningChance = 0.0166f;

    /// <summary>
    /// Infection warnings shown as popups
    /// </summary>
    [DataField("infectionWarnings")]
    public List<string> InfectionWarnings = new()
    {
        "zombie-infection-warning",
        "zombie-infection-underway"
    };

    /// <summary>
    /// Minimum time this zombie victim will lie dead before rising as a zombie.
    /// </summary>
    [DataField("deadMinTurnTime"), ViewVariables(VVAccess.ReadWrite)]
    public float DeadMinTurnTime = 10.0f;

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
