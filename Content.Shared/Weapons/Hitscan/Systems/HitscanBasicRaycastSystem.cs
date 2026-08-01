using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Player;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicRaycastSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISharedAdminLogManager _log = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly RequireProjectileTargetSystem _requireTarget = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<HitscanBasicVisualsComponent> _visualsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _visualsQuery = GetEntityQuery<HitscanBasicVisualsComponent>();

        SubscribeLocalEvent<HitscanBasicRaycastComponent, HitscanTraceEvent>(OnHitscanFired);
    }

    private void OnHitscanFired(Entity<HitscanBasicRaycastComponent> ent, ref HitscanTraceEvent args)
    {
        var shooter = args.Shooter ?? args.Gun;
        var mapCords = _transform.ToMapCoordinates(args.FromCoordinates);
        var target = args.Target;
        var ignored = args.IgnoredEntities;
        RayCastResults? result;
        if (target == shooter)
        {
            result = new RayCastResults(0f, mapCords.Position, shooter);
        }
        else
        {
            var ray = new CollisionRay(mapCords.Position, args.ShotDirection, (int) ent.Comp.CollisionMask);
            var rayCastResults = _physics.IntersectRay(mapCords.MapId, ray, ent.Comp.MaxDistance, shooter, false);

            // If you are in a container, use the raycast result
            // Otherwise:
            //  1.) Hit the first entity that you targeted.
            //  2.) Hit the first entity that doesn't require you to aim at it specifically to be hit.
            result = _container.IsEntityOrParentInContainer(shooter)
                ? rayCastResults.FirstOrNull(hit => ignored?.Contains(hit.HitEntity) != true)
                : rayCastResults.FirstOrNull(hit =>
                    ignored?.Contains(hit.HitEntity) != true &&
                    (hit.HitEntity == target || !RequiresExplicitTarget(hit.HitEntity)));
        }

        var distanceTried = result?.Distance ?? ent.Comp.MaxDistance;

        // DS14-start: aggregate the visual trace and render it after reflection handling.
        var isRoot = false;
        if (args.OutputTrace == null)
        {
            args.OutputTrace = [];
            isRoot = true;
        }

        if (GenerateTraceStep(args.FromCoordinates, distanceTried, args.ShotDirection.ToAngle(), result?.HitEntity) is { } trace)
            args.OutputTrace.Add(trace);
        // DS14-end

        // Do visuals without an event. They should always happen and putting it on the attempt event is weird!
        // If more stuff gets added here, it should probably be turned into an event.
        // DS14: visuals are fired after the hit attempt so reflected traces can be rendered together.

        // Admin logging
        if (result?.HitEntity != null)
        {
            _log.Add(LogType.HitScanHit,
                $"{ToPrettyString(shooter):user} hit {ToPrettyString(result.Value.HitEntity):target}"
                + $" using {ToPrettyString(args.Gun):entity}.");
        }

        var data = new HitscanRaycastFiredData
        {
            ShotDirection = args.ShotDirection,
            Gun = args.Gun,
            Shooter = args.Shooter,
            Target = target,
            PredictionId = args.PredictionId,
            HitEntity = result?.HitEntity,
            OutputTrace = args.OutputTrace,
            IgnoredEntities = ignored,
            HitPosition = result is { } hit ? new MapCoordinates(hit.HitPos, mapCords.MapId) : null,
        };

        var attemptEvent = new AttemptHitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
        {
            if (isRoot)
                FireEffects(ent.Owner, args.OutputTrace, args.Shooter, args.PredictionId);

            return;
        }

        var hitEvent = new HitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref hitEvent);

        if (isRoot)
            FireEffects(ent.Owner, args.OutputTrace, args.Shooter, args.PredictionId);
    }

    /// <summary>
    /// Builds one visual trace using the local physics state without raising any hit, damage, or effect events.
    /// </summary>
    public HitscanTrace? BuildVisualTrace(
        Entity<HitscanBasicRaycastComponent> ent,
        EntityCoordinates fromCoordinates,
        Vector2 shotDirection,
        EntityUid shooter,
        EntityUid? target)
    {
        var mapCoords = _transform.ToMapCoordinates(fromCoordinates);
        if (mapCoords.MapId == MapId.Nullspace || shotDirection.LengthSquared() <= 0.0001f)
            return null;

        var direction = shotDirection.Normalized();
        if (target == shooter)
            return GenerateTraceStep(fromCoordinates, 0f, direction.ToAngle(), shooter);

        var ray = new CollisionRay(mapCoords.Position, direction, (int) ent.Comp.CollisionMask);
        var rayCastResults = _physics.IntersectRay(mapCoords.MapId, ray, ent.Comp.MaxDistance, shooter, false);
        var result = _container.IsEntityOrParentInContainer(shooter)
            ? rayCastResults.FirstOrNull()
            : rayCastResults.FirstOrNull(hit =>
                hit.HitEntity == target || !RequiresExplicitTarget(hit.HitEntity));

        var distance = result?.Distance ?? ent.Comp.MaxDistance;
        return GenerateTraceStep(fromCoordinates, distance, direction.ToAngle(), result?.HitEntity);
    }

    private bool RequiresExplicitTarget(EntityUid uid)
    {
        return TryComp<RequireProjectileTargetComponent>(uid, out var requireTarget) &&
               _requireTarget.RequiresExplicitTarget((uid, requireTarget));
    }

    // DS14-start: hitscan trace visuals.
    private HitscanTrace? GenerateTraceStep(EntityCoordinates fromCoordinates, float distance, Angle shotAngle, EntityUid? entity = null)
    {
        var fromXform = Transform(fromCoordinates.EntityId);

        // We'll get the effects relative to the grid / map of the firer
        // Look you could probably optimise this a bit with redundant transforms at this point.

        var gridUid = fromXform.GridUid;
        if (gridUid != fromCoordinates.EntityId && TryComp(gridUid, out TransformComponent? gridXform))
        {
            var (_, gridRot, gridInvMatrix) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
            var map = _transform.ToMapCoordinates(fromCoordinates);
            fromCoordinates = new EntityCoordinates(gridUid.Value, Vector2.Transform(map.Position, gridInvMatrix));
            shotAngle -= gridRot;
        }
        else
        {
            var mapCoords = _transform.ToMapCoordinates(fromCoordinates);
            var mapEnt = Transform(fromCoordinates.EntityId).MapUid;
            if (mapEnt == null)
                return null;

            fromCoordinates = new EntityCoordinates(mapEnt.Value, mapCoords.Position);
        }

        var shotVec = shotAngle.ToVec().Normalized();

        return new HitscanTrace
        {
            Angle = shotAngle,
            Distance = distance,
            MuzzleCoordinates = distance > 1f ? GetNetCoordinates(fromCoordinates.Offset(shotVec / 2f)) : null,
            TravelCoordinates = distance > 1f ? GetNetCoordinates(fromCoordinates.Offset(shotVec * (distance + 0.5f) / 2f)) : null,
            ImpactCoordinates = GetNetCoordinates(fromCoordinates.Offset(shotVec * distance)),
            ImpactedEnt = GetNetEntity(entity),
        };
    }

    private void FireEffects(EntityUid hitscanUid, List<HitscanTrace> traces, EntityUid? shooter, uint predictionId)
    {
        if (traces.Count == 0 || !_visualsQuery.TryComp(hitscanUid, out var vizComp))
            return;

        var filter = Filter.Empty();
        foreach (var trace in traces)
        {
            var coords = GetCoordinates(trace.MuzzleCoordinates ?? trace.ImpactCoordinates);
            if (!coords.IsValid(EntityManager))
                continue;

            // DS14-start
            // Filter.Pvs ignores session view subscriptions used by remote eyes.
            var mapCoords = _transform.ToMapCoordinates(coords);
            filter.Merge(Filter.Empty().AddPlayersByPvs(mapCoords, entManager: EntityManager)
                .AddPlayersByViewSubscriptions(mapCoords, entityManager: EntityManager));
            // DS14-end
        }

        if (filter.Count == 0)
            return;

        if (vizComp.Bullet == null)
        {
            var sprites = new List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier Sprite, float Distance)>();

            foreach (var trace in traces)
            {
                if (trace.Distance >= 1f)
                {
                    if (vizComp.MuzzleFlash != null && trace.MuzzleCoordinates is { } muzzleCoordinates)
                        sprites.Add((muzzleCoordinates, trace.Angle, vizComp.MuzzleFlash, 1f));

                    if (vizComp.TravelFlash != null && trace.TravelCoordinates is { } travelCoordinates)
                        sprites.Add((travelCoordinates, trace.Angle, vizComp.TravelFlash, trace.Distance - 1.5f));
                }

                if (vizComp.ImpactFlash != null)
                    sprites.Add((trace.ImpactCoordinates, trace.Angle.FlipPositive(), vizComp.ImpactFlash, 1f));
            }

            if (sprites.Count == 0)
                return;

            RaiseNetworkEvent(new SharedGunSystem.HitscanEvent
            {
                Sprites = sprites,
                Traces = traces,
                MuzzleFlash = vizComp.MuzzleFlash,
                TravelFlash = vizComp.TravelFlash,
                ImpactFlash = vizComp.ImpactFlash,
                Speed = vizComp.Speed,
                Shooter = GetNetEntity(shooter),
                PredictionId = predictionId,
            }, filter);

            return;
        }

        RaiseNetworkEvent(new SharedGunSystem.HitscanEvent
        {
            Traces = traces,
            MuzzleFlash = vizComp.MuzzleFlash,
            TravelFlash = vizComp.TravelFlash,
            ImpactFlash = vizComp.ImpactFlash,
            Bullet = vizComp.Bullet,
            BulletLight = GetLightVisual(hitscanUid),
            Speed = vizComp.Speed,
            Shooter = GetNetEntity(shooter),
            PredictionId = predictionId,
        }, filter);
    }

    private HitscanLightVisual? GetLightVisual(EntityUid hitscanUid)
    {
        if (!_lights.TryGetLight(hitscanUid, out var light) || !light.Enabled)
            return null;

        return new HitscanLightVisual
        {
            Color = light.Color,
            Radius = light.Radius,
            Energy = light.Energy,
            Softness = light.Softness,
            Falloff = light.Falloff,
            CurveFactor = light.CurveFactor,
            CastShadows = light.CastShadows,
            Offset = light.Offset,
        };
    }
    // DS14-end
}
