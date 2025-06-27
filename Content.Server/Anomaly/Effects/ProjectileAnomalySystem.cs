using System.Numerics;
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
    [Dependency] private readonly SharedMapSystem _map = default!;

    private EntityQuery<TransformComponent> _xFormQuery;
    private EntityQuery<MobStateComponent> _mobQuery;

    /// <summary> Pre-allocated collection for calculating entities in range. </summary>
    private readonly HashSet<EntityUid> _inRange = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ProjectileAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);

        _xFormQuery = GetEntityQuery<TransformComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
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
        var projectileCount = (int)MathF.Round(MathHelper.Lerp(component.MinProjectiles, component.MaxProjectiles, severity));

        var xform = _xFormQuery.GetComponent(uid);

        _inRange.Clear();
        _lookup.GetEntitiesInRange(uid, component.ProjectileRange * severity, _inRange, LookupFlags.Dynamic);

        if (_inRange.Count == 0)
            return;

        var priority = new List<EntityUid>();
        foreach (var entity in _inRange)
        {
            if (_mobQuery.HasComponent(entity))
                priority.Add(entity);
        }

        Log.Debug($"shots: {projectileCount}");
        while (projectileCount > 0)
        {
            Log.Debug($"{projectileCount}");
            var target = priority.Count > 0
                ? _random.PickAndTake(priority)
                : _random.Pick(_inRange);

            var targetXForm= _xFormQuery.GetComponent(target);
            var targetCoords = targetXForm.Coordinates.Offset(_random.NextVector2(0.5f));

            ShootProjectile(
                uid,
                component,
                xform.Coordinates,
                targetCoords,
                severity
            );
            projectileCount--;
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
        var mapPos = _xform.ToMapCoordinates(coords);

        var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                ? _xform.WithEntityId(coords, gridUid)
                : new(_map.GetMapOrInvalid(mapPos.MapId), mapPos.Position);

        var ent = Spawn(component.ProjectilePrototype, spawnCoords);
        var direction = _xform.ToMapCoordinates(targetCoords).Position - mapPos.Position;

        if (!TryComp<ProjectileComponent>(ent, out var comp))
            return;

        comp.Damage *= severity;

        _gunSystem.ShootProjectile(ent, direction, Vector2.Zero, uid, uid, component.ProjectileSpeed);
    }
}
