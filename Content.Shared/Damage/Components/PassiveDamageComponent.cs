using Content.Shared.Mobs;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Passively damages the entity on a specified interval.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PassiveDamageComponent : Component
{
    /// <summary>
    /// List of passive damage entries applied to this entity.
    /// </summary>
    [DataField]
    public List<PassiveDamageEntry> PassiveDamageList = new();
    [DataField("nextDamage", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDamage;
    /// <summary>
    /// The total damage sum from all entries that will be applied to the entity, calculated every interval. Assigning a value to this will not do anything, and having it here is probably unrobust of me.
    /// </summary>
    public DamageSpecifier DamageSum = new();
}

/// <summary>
/// Represents one passive damage entry.
/// Multiple of these can be on an entity at once, allowing for parellel passive damage.
/// </summary>
[DataDefinition]
public partial struct PassiveDamageEntry
{
    /// <summary>
    /// What entity states the passive damage will apply in
    /// </summary>
    [DataField]
    public List<MobState> AllowedStates;

    /// <summary>
    /// Damage / Healing per interval dealt to the entity every interval
    /// </summary>
    [DataField]
    public DamageSpecifier Damage;

    /// <summary>
    /// Delay between damage events in seconds. Unused, and 1f is magic numbered in instead. Not very cash money.
    /// </summary>
    [DataField]
    public float Interval;

    /// <summary>
    /// The maximum HP the damage will be given to. If 0, disabled.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageCap;

    /// <summary>
    /// Damage cap for this entry only cares about damage of the damage type(s) present in this entry, and not the total damage on the entity. False by default.
    /// </summary>
    [DataField]
    public bool SpecificDamageCap;
}
