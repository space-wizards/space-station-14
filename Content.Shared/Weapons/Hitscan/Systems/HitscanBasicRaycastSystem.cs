using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicRaycastSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISharedAdminLogManager _log = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicRaycastComponent, HitscanTraceEvent>(OnHitscanFired);
    }

    private void OnHitscanFired(Entity<HitscanBasicRaycastComponent> ent, ref HitscanTraceEvent args)
    {
        var shooter = args.Shooter ?? args.Gun;
        var mapCords = _transform.ToMapCoordinates(args.FromCoordinates);
        var ray = new CollisionRay(mapCords.Position, args.ShotDirection, (int) ent.Comp.CollisionMask);
        var rayCastResults = _physics.IntersectRay(mapCords.MapId, ray, ent.Comp.MaxDistance, shooter, false);

        var target = args.Target;
        // If you are in a container, use the raycast result
        // Otherwise:
        //  1.) Hit the first entity that you targeted.
        //  2.) Hit the first entity that doesn't require you to aim at it specifically to be hit.
        var result = _container.IsEntityOrParentInContainer(shooter)
            ? rayCastResults.FirstOrNull()
            : rayCastResults.FirstOrNull(hit => hit.HitEntity == target
                                                || CompOrNull<RequireProjectileTargetComponent>(hit.HitEntity)?.Active != true);

        var trace = new HitscanRaycastFiredEvent
        {
            FromCoordinates = args.FromCoordinates,
            ShotDirection = args.ShotDirection,
            Gun = args.Gun,
            Shooter = args.Shooter,
            HitEntity = result?.HitEntity,
            DistanceTried = result?.Distance ?? ent.Comp.MaxDistance,
        };

        RaiseLocalEvent(ent, ref trace);

        if (result?.HitEntity == null)
            return;

        _log.Add(LogType.HitScanHit,
            $"{ToPrettyString(shooter):user} hit {ToPrettyString(result.Value.HitEntity):target}"
            + $" using {ToPrettyString(args.Gun):entity}.");
    }
}
