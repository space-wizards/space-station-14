using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Should the entity take damage / be stunned if colliding at a speed above MinimumSpeed?
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DamageOnHighSpeedImpactSystem))]
public sealed partial class DamageOnHighSpeedImpactComponent : Component
{
    [DataField("minimumSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float MinimumSpeed = 20f;

    [DataField("speedDamageFactor"), ViewVariables(VVAccess.ReadWrite)]
    public float SpeedDamageFactor = 0.5f;

    [DataField("soundHit", required: true), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundHit = default!;

    [DataField("stunChance"), ViewVariables(VVAccess.ReadWrite)]
    public float StunChance = 0.25f;

    [DataField("stunMinimumDamage"), ViewVariables(VVAccess.ReadWrite)]
    public int StunMinimumDamage = 10;

    [DataField("stunSeconds"), ViewVariables(VVAccess.ReadWrite)]
    public float StunSeconds = 1f;

    [DataField("damageCooldown"), ViewVariables(VVAccess.ReadWrite)]
    public float DamageCooldown = 2f;

    [DataField("lastHit", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? LastHit;

    [DataField("damage", required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;
}
