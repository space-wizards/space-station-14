using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Projectiles.Components;
using Content.Shared.Explosion;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Projectiles;

public sealed class ProjectileAoeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _entities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AreaOnImpactComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid uid, AreaOnImpactComponent comp, ref ProjectileHitEvent args)
    {
        // If an explosion prototype is configured, spawn an explosion centered on the projectile.
        if (comp.ExplosionPrototype is { } proto)
        {
            var coords = _transform.GetMapCoordinates(uid);

            // Convert tile radius to a total intensity using the explosion system.
            var totalIntensity = _explosionSystem.RadiusToIntensity(comp.TileRadius, comp.IntensitySlope, comp.MaxTileIntensity);

            // Queue explosion; use the projectile as the cause. The explosion system handles tile effects and damage.
            _explosionSystem.QueueExplosion(coords, proto, totalIntensity, comp.IntensitySlope, comp.MaxTileIntensity, uid, comp.TileBreakScale, comp.MaxTileBreak);

            return;
        }

        // Otherwise, if direct damage is configured, apply that damage to entities in range.
        if (comp.Damage is { } dmg)
        {
            _entities.Clear();
            _lookup.GetEntitiesInRange(uid, comp.TileRadius, _entities, LookupFlags.Dynamic | LookupFlags.Sundries);

            foreach (var ent in _entities)
            {
                // Skip the projectile entity itself.
                if (ent == uid)
                    continue;

                _damageableSystem.TryChangeDamage(ent, dmg, comp.IgnoreResistances, origin: args.Shooter);
            }
        }
    }
}
