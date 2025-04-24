using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Zombies;

/// <summary>
/// Temporary because diseases suck.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PendingZombieComponent : Component
{
    /// <summary>
    /// Damage dealt every second to infected individuals.
    /// </summary>
    [DataField("damage")] public DamageSpecifier Damage = new()
    {
        DamageDict = new ()
        {
            { "Poison", 0.4 },
        }
    };

    /// <summary>
    /// A multiplier for <see cref="Damage"/> applied when the entity is in critical condition.
    /// </summary>
    [DataField("critDamageMultiplier")]
    public float CritDamageMultiplier = 10f;

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// The amount of time left before the infected begins to take damage.
    /// </summary>
    [DataField("gracePeriod"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GracePeriod = TimeSpan.Zero;

    /// <summary>
    /// The minimum amount of time initial infected have before they start taking infection damage.
    /// </summary>
    [DataField]
    public TimeSpan MinInitialInfectedGrace = TimeSpan.FromMinutes(12.5f);

    /// <summary>
    /// The maximum amount of time initial infected have before they start taking damage.
    /// </summary>
    [DataField]
    public TimeSpan MaxInitialInfectedGrace = TimeSpan.FromMinutes(15f);

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
}
