using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
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
        ShootProjectilesAtEntities(uid, component, args.Severity * args.PowerModifier);
    }

    private void OnSupercritical(EntityUid uid, ProjectileAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        ShootProjectilesAtEntities(uid, component, args.PowerModifier);
    }

    private void ShootProjectilesAtEntities(EntityUid uid, ProjectileAnomalyComponent component, float severity)
    {
        var projectileCount = (int) MathF.Round(MathHelper.Lerp(component.MinProjectiles, component.MaxProjectiles, severity));
        var xformQuery = GetEntityQuery<TransformComponent>();
        var mobQuery = GetEntityQuery<MobStateComponent>();
        var xform = xformQuery.GetComponent(uid);

        var inRange = _lookup.GetEntitiesInRange(uid, component.ProjectileRange * severity, LookupFlags.Dynamic).ToList();
        _random.Shuffle(inRange);
        var priority = new List<EntityUid>();
        foreach (var entity in inRange)
        {
            if (mobQuery.HasComponent(entity))
                priority.Add(entity);
        }

        Log.Debug($"shots: {projectileCount}");
        while (projectileCount > 0)
        {
            Log.Debug($"{projectileCount}");
            var target = priority.Any()
                ? _random.PickAndTake(priority)
                : _random.Pick(inRange);

            var targetCoords = xformQuery.GetComponent(target).Coordinates.Offset(_random.NextVector2(0.5f));

            ShootProjectile(
                uid, component,
                xform.Coordinates,
                targetCoords,
                severity);
            projectileCount--;
        }
    }

    private void ShootProjectile(
        EntityUid uid,
        ProjectileAnomalyComponent component,
        EntityCoordinates coords,
        EntityCoordinates targetCoords,
        float severity)
    {
        var mapPos = _xform.ToMapCoordinates(coords);

        var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                ? _xform.WithEntityId(coords, gridUid)
                : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

        var ent = Spawn(component.ProjectilePrototype, spawnCoords);
        var direction = _xform.ToMapCoordinates(targetCoords).Position - mapPos.Position;

        if (!TryComp<ProjectileComponent>(ent, out var comp))
            return;

        comp.Damage *= severity;

        _gunSystem.ShootProjectile(ent, direction, Vector2.Zero, uid, uid, component.ProjectileSpeed);
    }
}
