// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Weapons.Ranged.Upgrades;

[RegisterComponent, NetworkedComponent]
public sealed partial class ExplosivePkaUpgradeComponent : Component
{
    [DataField]
    public float Radius = 2.5f;

    [DataField]
    public float MaxRange = 8f;

    [DataField]
    public float ExplosionIntensity = 3f;

    [DataField]
    public float ExplosionSlope = 1f;

    [DataField]
    public float ExplosionMaxIntensity = 2f;

    [DataField]
    public DamageSpecifier HumanDamage = Damage(0.5f);

    [DataField]
    public DamageSpecifier CreatureDamage = Damage(80);

    [DataField]
    public DamageSpecifier BossDamage = Damage(125);

    [DataField]
    public DamageSpecifier BulletDamage = Damage(20);

    private static DamageSpecifier Damage(float amount)
    {
        return new DamageSpecifier
        {
            DamageDict = new()
            {
                { "Blunt", FixedPoint2.New(amount) },
            },
        };
    }
}

[RegisterComponent]
public sealed partial class ExplosivePkaProjectileComponent : Component
{
    [DataField]
    public float Radius = 2.5f;

    [DataField]
    public float ExplosionIntensity = 3f;

    [DataField]
    public float ExplosionSlope = 1f;

    [DataField]
    public float ExplosionMaxIntensity = 2f;

    [DataField]
    public DamageSpecifier HumanDamage = new();

    [DataField]
    public DamageSpecifier CreatureDamage = new();

    [DataField]
    public DamageSpecifier BossDamage = new();
}
