using System.Linq;
using Content.Server.Anomaly.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="ProjectileAnomalyComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class ProjectileAnomalySystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ProjectileAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(EntityUid uid, ProjectileAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        ShootProjectilesAtEntities(uid, component, args.Severity);
    }

    private void OnSupercritical(EntityUid uid, ProjectileAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        ShootProjectilesAtEntities(uid, component, 1.0f);
    }

    private void ShootProjectilesAtEntities(EntityUid uid, ProjectileAnomalyComponent component, float severity)
    {
        var xform = Transform(uid);
        var projectileCount = (int) MathF.Round(MathHelper.Lerp(component.MinProjectiles, component.MaxProjectiles, severity));
        var mobQuery = GetEntityQuery<MobStateComponent>();
        var inRange = _lookup.GetEntitiesInRange(uid, component.ProjectileRange * severity, LookupFlags.Dynamic).ToList();
        _random.Shuffle(inRange);

        var priority = inRange.Where(mobQuery.HasComponent).ToList();
        var randomProjectiles = projectileCount - priority.Count;

        var toShoot = priority.Take(Math.Min(projectileCount, priority.Count)).ToList();
        for (var i = 0; i < randomProjectiles; i++)
        {
            toShoot.Add(_random.PickAndTake(inRange));
        }

        foreach (var entity in toShoot)
        {
            var targetCoords = Transform(entity).Coordinates.Offset(_random.NextVector2(0.5f));

            ShootProjectile(
                uid, component,
                xform.Coordinates,
                targetCoords,
                severity
            );
        }
    }

    private void ShootProjectile(
        EntityUid uid,
        ProjectileAnomalyComponent component,
        EntityCoordinates coords,
        EntityCoordinates targetCoords,
        float severity)
    {
        var mapPos = coords.ToMap(EntityManager, _xform);

        var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                ? coords.WithEntityId(gridUid, EntityManager)
                : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

        var ent = Spawn(component.ProjectilePrototype, spawnCoords);
        var direction = targetCoords.ToMapPos(EntityManager, _xform) - mapPos.Position;

        if (!TryComp<ProjectileComponent>(ent, out var comp))
            return;

        comp.Damage *= severity;

        _gunSystem.ShootProjectile(ent, direction, Vector2.Zero, uid, uid, component.MaxProjectileSpeed * severity);
    }
}
