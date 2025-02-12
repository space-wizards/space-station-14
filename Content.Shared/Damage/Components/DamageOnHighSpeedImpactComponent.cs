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
    [DataField, ViewVariables]
    public float MinimumSpeed = 20f;

    [DataField, ViewVariables]
    public float SpeedDamageFactor = 0.5f;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), ViewVariables(required: true), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundHit = default!;

    [DataField, ViewVariables]
    public float StunChance = 0.25f;

    [DataField, ViewVariables]
    public int StunMinimumDamage = 10;

    [DataField, ViewVariables]
    public float StunSeconds = 1f;

    [DataField, ViewVariables]
    public float DamageCooldown = 2f;

    [DataField("lastHit", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? LastHit;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), ViewVariables(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;
}
