using Content.Shared.Adventurer.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Adventurer.Components;

/// <summary>
/// Marks an entity as a member of an adventuring party.
/// Grants a D&amp;D-style armor class: attacks against this entity roll a d20 and only
/// deal damage if the roll meets or beats <see cref="ArmorClass"/>. Also restricts
/// the entity to adventurer-approved guns and optionally adjusts mob thresholds.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(AdventurerSystem))]
public sealed partial class AdventurerComponent : Component
{
    /// <summary>
    /// The armor class of this adventurer. Incoming attack rolls (1d20) must meet or
    /// beat this value to deal damage, so higher is better. Values of 1 or lower mean
    /// every attack lands; 21+ would block everything, so don't do that.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ArmorClass = 10;

    /// <summary>
    /// If set, overrides the critical (down) damage threshold of the mob, i.e. its effective HP.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2? CritThreshold;

    /// <summary>
    /// If set, overrides the dead damage threshold of the mob.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2? DeadThreshold;

    /// <summary>
    /// Guns matching this whitelist can still be fired by the adventurer.
    /// Anything else is too much strange technology for them.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? GunWhitelist = new()
    {
        Tags = new() { "AdventurerWeapon" },
    };

    /// <summary>
    /// Popup shown when the adventurer tries to fire a non-whitelisted gun.
    /// </summary>
    [DataField]
    public LocId GunFailedMessage = "adventurer-gun-fail-message";

    /// <summary>
    /// Popup shown when an attack roll fails to beat the armor class.
    /// </summary>
    [DataField]
    public LocId AttackBlockedMessage = "adventurer-ac-blocked-message";

    /// <summary>
    /// Cosmetic die spawned when an attack roll happens. Should have a
    /// <c>Dice</c> component with 20 sides and a <c>TimedDespawn</c> so it cleans itself up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId DiePrototype = "AdventurerFateDie";

    /// <summary>
    /// Magnitude of the random impulse applied to the cosmetic die so it skitters a little.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DieImpulseStrength = 1.5f;

    /// <summary>
    /// Minimum time between cosmetic die spawns. Rolls still happen for every attack;
    /// this only limits the visual so rapid fire doesn't flood the map with dice.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DieCooldown = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Next time a cosmetic die may be spawned. Server-side bookkeeping only.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextDieTime;
}
