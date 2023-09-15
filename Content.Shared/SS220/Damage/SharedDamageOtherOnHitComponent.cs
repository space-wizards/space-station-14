// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Shared.SS220.Damage;

/// <summary>
/// Raised directly on an entity that hit something.
/// Here you can modify the damage, before it applies.
/// </summary>
public sealed class GetDamageOtherOnHitEvent : HandledEntityEventArgs
{
    /// <summary>
    /// Entity that hit something.
    /// </summary>
    public readonly NetEntity HitEntity;

    public readonly NetEntity Target;

    public DamageSpecifier Damage;

    public bool IgnoreResistances;

    public GetDamageOtherOnHitEvent(NetEntity hitEntity, NetEntity target, DamageSpecifier damage, bool ignoreResistances)
    {
        HitEntity = hitEntity;
        Target = target;
        Damage = damage;
        IgnoreResistances = ignoreResistances;
    }
}
