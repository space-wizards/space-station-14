using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Physics;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

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

        SubscribeLocalEvent<HitscanBasicRaycastComponent, HitscanFiredEvent>(OnHitscanFired);
    }

    private void OnHitscanFired(Entity<HitscanBasicRaycastComponent> hitscan, ref HitscanFiredEvent args)
    {
        var mapCords = _transform.ToMapCoordinates(args.FromCoordinates);
        var ray = new CollisionRay(mapCords.Position, args.ShotDirection, (int) CollisionGroup.Opaque);
        var rayCastResults = _physics.IntersectRay(mapCords.MapId, ray, hitscan.Comp.MaxDistance, args.Shooter, false).ToList();

        RayCastResults? result = null;

        if (rayCastResults.Any())
            result = rayCastResults[0];

        // Check if laser is shot from in a container
        if (!_container.IsEntityOrParentInContainer(args.Shooter))
        {
            // Checks if the laser should pass over unless targeted by its user
            foreach (var collide in rayCastResults)
            {
                if (collide.HitEntity != args.Target && CompOrNull<RequireProjectileTargetComponent>(collide.HitEntity)?.Active == true)
                    continue;

                result = collide;
                break;
            }
        }

        var raycastEvent = new HitscanRaycastResultsEvent
        {
            RaycastResults = result,
            DistanceTried = hitscan.Comp.MaxDistance,
            FromCoordinates = args.FromCoordinates,
            ShotDirection = args.ShotDirection,
        };

        RaiseLocalEvent(hitscan, ref raycastEvent);

        if (result == null)
            return;

        var hitEvent = new HitscanHitEntityEvent
        {
            FromCoordinates = args.FromCoordinates,
            ShotDirection = args.ShotDirection,
            GunUid = args.GunUid,
            Shooter = args.Shooter,
            HitEntity = result.Value.HitEntity,
        };

        RaiseLocalEvent(hitscan, ref hitEvent);

        _log.Add(LogType.HitScanHit, $"{ToPrettyString(args.Shooter):user} hit {hitEvent.HitEntity:target} using hitscan.");

    }
}
