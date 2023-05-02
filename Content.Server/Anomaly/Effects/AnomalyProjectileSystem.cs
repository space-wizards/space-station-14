using Content.Server.Atmos.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="AnomalyProjectileComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class AnomalyProjectileSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyProjectileComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<AnomalyProjectileComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(EntityUid uid, AnomalyProjectileComponent component, ref AnomalyPulseEvent args)
    {
        ShootProjectilesAtEntites(uid, component, args.Severity, args.Stability);
    }

    private void OnSupercritical(EntityUid uid, AnomalyProjectileComponent component, ref AnomalySupercriticalEvent args)
    {
        ShootProjectilesAtEntites(uid, component, 1.0f, 1.0f);
    }

    private void ShootProjectilesAtEntites(EntityUid uid, AnomalyProjectileComponent component, float severity, float stability)
    {
        var xform = Transform(uid);
        var projectilesShot = 0;
        var range = Math.Abs(component.ProjectileRange * stability); // Apparently this shit can be a negative somehow?

        foreach (var entity in _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic))
        {
            if (projectilesShot >= component.MaxProjectiles * severity)
                return;

            // Living entities are more likely to be shot at than non living
            if (!HasComp<MobStateComponent>(entity) && !_random.Prob(component.TargetNonLivingChance))
                continue;

            var targetCoords = Transform(entity).Coordinates.Offset(_random.NextVector2(-1, 1));

            ShootProjectile(
                uid, component,
                xform.Coordinates,
                targetCoords,
                severity
            );
            projectilesShot++;
        }
    }

    private void ShootProjectile(
        EntityUid uid,
        AnomalyProjectileComponent component,
        EntityCoordinates coords,
        EntityCoordinates targetCoords,
        float severity
        )
    {
        var mapPos = coords.ToMap(EntityManager, _xform);

        var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var grid)
                ? coords.WithEntityId(grid.Owner, EntityManager)
                : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

        var ent = Spawn(component.ProjectilePrototype, spawnCoords);
        var direction = targetCoords.ToMapPos(EntityManager, _xform) - mapPos.Position;

        if (!TryComp<ProjectileComponent>(ent, out var comp))
            return;

        comp.Damage *= severity;

        _gunSystem.ShootProjectile(ent, direction, Vector2.Zero, uid, component.MaxProjectileSpeed * severity);
    }
}
