using Content.Server.Damage.Systems;
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Damage.Components;

[RegisterComponent]
[Access(typeof(DamageOnHoldingSystem))]
public sealed class DamageOnHoldingComponent : Component
{
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>
    /// Damage per interval dealt to entity holding the entity with this component
    /// </summary>
    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Delay between damage events in seconds
    /// </summary>
    [DataField("interval"), ViewVariables(VVAccess.ReadWrite)]
    public float Interval = 1f;

    [DataField("nextDamage", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextDamage = TimeSpan.Zero;
}
