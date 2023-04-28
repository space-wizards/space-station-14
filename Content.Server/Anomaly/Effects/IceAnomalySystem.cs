using Content.Server.Weapons.Ranged.Systems;
using Content.Server.Anomaly.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Interaction;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Content.Shared.Projectiles;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="IceAnomalyComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class IceAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entman = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<IceAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<IceAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(EntityUid uid, IceAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xform = Transform(uid);

        foreach(var entity in _lookup.GetEntitiesInRange(uid, component.ProjectileRange * args.Stability, LookupFlags.Dynamic))
        {
            if (!HasComp<MobStateComponent>(entity))
                continue;
            ShootProjectile(
                uid, component,
                Transform(uid).Coordinates,
                Transform(entity).Coordinates,
                args.Severity
            );
        }
    }

    private void ShootProjectile(
        EntityUid uid,
        IceAnomalyComponent component,
        EntityCoordinates coords,
        EntityCoordinates targetCoords,
        float severity
        )
    {
        var mapPos = coords.ToMap(_entman, _xform);

        EntityCoordinates spawnCoords = _mapManager.TryFindGridAt(mapPos, out var grid)
                ? coords.WithEntityId(grid.Owner, EntityManager)
                : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

        var ent = Spawn(component.ProjectilePrototype, spawnCoords);
        var direction = (targetCoords.ToMapPos(_entman, _xform) - mapPos.Position);

        if (!TryComp<ProjectileComponent>(ent, out ProjectileComponent? comp))
            return;

        foreach(var key in component.ProjectileDamage.DamageDict.Keys)
        {
            comp.Damage.DamageDict[key] = component.ProjectileDamage.DamageDict[key] * severity;
        }

        _gunSystem.ShootProjectile(ent, direction, Vector2.Zero, uid, component.MaxProjectileSpeed * severity);
    }

    private void OnSupercritical(EntityUid uid, IceAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        var grid = xform.GridUid;
        var map = xform.MapUid;

        var indices = _xform.GetGridOrMapTilePosition(uid, xform);
        var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);

        if (mixture == null)
            return;
        mixture.AdjustMoles(component.SupercriticalGas, component.SupercriticalMoleAmount);
        if (grid is { })
        {
            foreach (var ind in _atmosphere.GetAdjacentTiles(grid.Value, indices))
            {
                var mix = _atmosphere.GetTileMixture(grid, map, ind, true);
                if (mix is not { })
                    continue;

                mix.AdjustMoles(component.SupercriticalGas, component.SupercriticalMoleAmount);
                mix.Temperature += component.FreezeZoneExposeTemperature;
                _atmosphere.HotspotExpose(grid.Value, indices, component.FreezeZoneExposeTemperature, mix.Volume, uid, true);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IceAnomalyComponent, AnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var ice, out var anom, out var xform))
        {
            var grid = xform.GridUid;
            var map = xform.MapUid;
            var indices = _xform.GetGridOrMapTilePosition(ent, xform);
            var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);
            if (mixture is { })
            {
                mixture.Temperature += ice.HeatPerSecond * anom.Severity * frameTime;
            }

            if (grid != null && anom.Severity > ice.AnomalyFreezeZoneThreshold)
            {
                _atmosphere.HotspotExpose(grid.Value, indices, ice.FreezeZoneExposeTemperature, ice.AnomalyFreezeZoneThreshold, ent, true);
            }
        }
    }
}
