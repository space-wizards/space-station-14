using System.Numerics;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicVisualsComponent, HitscanRaycastResultsEvent>(OnHitscanHit, before: [ typeof(HitscanReflectSystem) ]);
    }

    private void OnHitscanHit(Entity<HitscanBasicVisualsComponent> hitscan, ref HitscanRaycastResultsEvent args)
    {
        var distance = args.RaycastResults?.Distance ?? args.DistanceTried;

        FireEffects(args.FromCoordinates, distance, args.ShotDirection.Normalized().ToAngle(), hitscan.Comp);
    }

    private void FireEffects(EntityCoordinates fromCoordinates, float distance, Angle angle, HitscanBasicVisualsComponent hitscan, EntityUid? hitEntity = null)
    {
        // Lord
        // Forgive me for the shitcode I am about to do
        // Effects tempt me not
        var sprites = new List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float scale)>();
        var fromXform = Transform(fromCoordinates.EntityId);

        // We'll get the effects relative to the grid / map of the firer
        // Look you could probably optimise this a bit with redundant transforms at this point.

        var gridUid = fromXform.GridUid;
        if (gridUid != fromCoordinates.EntityId && TryComp(gridUid, out TransformComponent? gridXform))
        {
            var (_, gridRot, gridInvMatrix) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
            var map = _transform.ToMapCoordinates(fromCoordinates);
            fromCoordinates = new EntityCoordinates(gridUid.Value, Vector2.Transform(map.Position, gridInvMatrix));
            angle -= gridRot;
        }
        else
        {
            angle -= _transform.GetWorldRotation(fromXform);
        }

        if (distance >= 1f)
        {
            if (hitscan.MuzzleFlash != null)
            {
                var coords = fromCoordinates.Offset(angle.ToVec().Normalized() / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, angle, hitscan.MuzzleFlash, 1f));
            }

            if (hitscan.TravelFlash != null)
            {
                var coords = fromCoordinates.Offset(angle.ToVec() * (distance + 0.5f) / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, angle, hitscan.TravelFlash, distance - 1.5f));
            }
        }

        if (hitscan.ImpactFlash != null)
        {
            var coords = fromCoordinates.Offset(angle.ToVec() * distance);
            var netCoords = GetNetCoordinates(coords);

            sprites.Add((netCoords, angle.FlipPositive(), hitscan.ImpactFlash, 1f));
        }

        if (sprites.Count > 0)
        {
            RaiseNetworkEvent(new SharedGunSystem.HitscanEvent
            {
                Sprites = sprites,
            }, Filter.Pvs(fromCoordinates, entityMan: EntityManager));
        }
    }
}
