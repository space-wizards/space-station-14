using Content.Shared.Mobs;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Passively damages the entity on a specified interval.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PassiveDamageComponent : Component
{
    /// <summary>
    /// The entitys' states that passive damage will apply in
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<MobState> AllowedStates = new();

    /// <summary>
    /// Damage / Healing per interval dealt to the entity every interval
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Delay between damage events in seconds
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The next time the damage should occur at.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamage;

    /// <summary>
    /// How long to pause the passive health change after damage has been taken.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan IntervalHaltOnDamageTaken;
}
