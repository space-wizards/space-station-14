using Content.Shared.Mobs;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PassiveDamageComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<MobState> AllowedStates = new();

    /// <summary>
    /// Damage per interval dealt (or healed) to the entity every interval
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Delay between damage events in seconds
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Interval = 1f;

    /// <summary>
    /// The maximum HP the damage will be given to. If null, disabled.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2? DamageCap;

    [DataField("nextDamage", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextDamage = TimeSpan.Zero;
}
