// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
using Content.Server.DeadSpace.Lavaland.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Lavaland.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Weapons.Ranged.Upgrades;
using Content.Shared.Humanoid;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Spawners;

namespace Content.Server.DeadSpace.Weapons.Ranged;

public sealed class ExplosivePkaUpgradeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExplosivePkaUpgradeComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ExplosivePkaProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnGunShot(Entity<ExplosivePkaUpgradeComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo is not { } projectile ||
                !TryComp<ProjectileComponent>(projectile, out var projectileComp))
                continue;

            projectileComp.Damage += ent.Comp.BulletDamage;
            var explosive = EnsureComp<ExplosivePkaProjectileComponent>(projectile);
            explosive.Radius = ent.Comp.Radius;
            explosive.HumanDamage = ent.Comp.HumanDamage;
            explosive.CreatureDamage = ent.Comp.CreatureDamage;
            explosive.BossDamage = ent.Comp.BossDamage;
            explosive.ExplosionIntensity = ent.Comp.ExplosionIntensity;
            explosive.ExplosionSlope = ent.Comp.ExplosionSlope;
            explosive.ExplosionMaxIntensity = ent.Comp.ExplosionMaxIntensity;

            if (TryComp<TimedDespawnComponent>(projectile, out var timed) &&
                TryComp<PhysicsComponent>(projectile, out var physics) &&
                physics.LinearVelocity.Length() > 0.01f)
                timed.Lifetime = ent.Comp.MaxRange / physics.LinearVelocity.Length();
        }
    }

    private void OnProjectileHit(Entity<ExplosivePkaProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (TryDamageFor(args.Target, ent.Comp, out var directDamage))
            args.Damage = directDamage;
        var center = _transform.GetMapCoordinates(args.Target);

        foreach (var victim in _lookup.GetEntitiesInRange(center, ent.Comp.Radius))
        {
            if (victim == args.Target)
                continue;

            var damage = TryDamageFor(victim, ent.Comp, out var specializedDamage)
                ? specializedDamage
                : ent.Comp.CreatureDamage;
            _damage.TryChangeDamage(victim, damage, true, origin: args.Shooter);
        }

        _explosion.QueueExplosion(
            center,
            "LavalandPkaVisual",
            ent.Comp.ExplosionIntensity,
            ent.Comp.ExplosionSlope,
            ent.Comp.ExplosionMaxIntensity,
            args.Shooter,
            tileBreakScale: 0f,
            maxTileBreak: 0,
            canCreateVacuum: false,
            addLog: false);
    }

    private bool TryDamageFor(
        EntityUid target,
        ExplosivePkaProjectileComponent component,
        out global::Content.Shared.Damage.DamageSpecifier damage)
    {
        if (HasComp<LavalandBossComponent>(target))
        {
            damage = component.BossDamage;
            return true;
        }

        if (HasComp<LavalandFaunaComponent>(target) || HasComp<NecromorfComponent>(target))
        {
            damage = component.CreatureDamage;
            return true;
        }

        if (HasComp<HumanoidAppearanceComponent>(target))
        {
            damage = component.HumanDamage;
            return true;
        }

        damage = default!;
        return false;
    }
}
