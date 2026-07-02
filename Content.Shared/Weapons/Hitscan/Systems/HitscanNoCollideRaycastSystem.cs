using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanNoCollideRaycastSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ISharedAdminLogManager _log = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly HitscanBasicEffectsSystem _hitscanBasicEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanNoCollideRaycastComponent, HitscanTraceEvent>(OnHitscanFired);
    }

    private void OnHitscanFired(Entity<HitscanNoCollideRaycastComponent> ent, ref HitscanTraceEvent args)
    {
        var shooter = args.Shooter ?? args.Gun;
        var mapCords = _transform.ToMapCoordinates(args.FromCoordinates);
        var ray = new CollisionRay(mapCords.Position, args.ShotDirection, (int) ent.Comp.CollisionMask);

        var rayCastResults = _physics.IntersectRay(mapCords.MapId, ray, ent.Comp.Range, shooter, false);

        var distanceTraveled = ent.Comp.Range;

        foreach (var result in rayCastResults)
        {
            var data = new HitscanRaycastFiredData
            {
                ShotDirection = args.ShotDirection,
                Gun = args.Gun,
                Shooter = args.Shooter,
                HitEntity = result.HitEntity,
            };

            _log.Add(LogType.HitScanHit,
                $"{ToPrettyString(shooter):user} hit {ToPrettyString(result.HitEntity):target}"
                + $" using {ToPrettyString(args.Gun):entity}.");

            var attemptEvent = new AttemptHitscanRaycastFiredEvent { Data = data };
            RaiseLocalEvent(ent, ref attemptEvent);

            if (attemptEvent.Cancelled)
            {
                distanceTraveled = result.Distance;
                break;
            }

            var hitEvent = new HitscanRaycastFiredEvent { Data = data };
            RaiseLocalEvent(ent, ref hitEvent);
        }

        _hitscanBasicEffects.FireEffects(args.FromCoordinates, distanceTraveled, args.ShotDirection.ToAngle(), ent.Owner);
    }
}
