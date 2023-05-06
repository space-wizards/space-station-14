using Content.Server.Anomaly.Components;
using Content.Server.Mind.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Anomaly.Components;
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
        var projectilesShot = 0;
        var range = component.ProjectileRange * severity;
        var mobQuery = GetEntityQuery<MindComponent>();

        foreach (var entity in _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic))
        {
            if (projectilesShot >= component.MaxProjectiles * severity)
                return;

            // Sentient entities are more likely to be shot at than non sentient
            if (!mobQuery.HasComponent(entity) && !_random.Prob(component.TargetNonSentientChance))
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
        ProjectileAnomalyComponent component,
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
