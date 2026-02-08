using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<HitscanBasicVisualsComponent> _visualsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _visualsQuery = GetEntityQuery<HitscanBasicVisualsComponent>();

        SubscribeLocalEvent<HitscanBasicEffectsComponent, HitscanDamageDealtEvent>(OnHitscanDamageDealt);
    }

    private void OnHitscanDamageDealt(Entity<HitscanBasicEffectsComponent> ent, ref HitscanDamageDealtEvent args)
    {
        if (Deleted(args.Target))
            return;

        if (ent.Comp.HitColor != null && args.DamageDealt.GetTotal() != 0)
        {
            _color.RaiseEffect(ent.Comp.HitColor.Value,
                new List<EntityUid> { args.Target },
                Filter.Pvs(args.Target, entityManager: EntityManager));
        }

        _gun.PlayImpactSound(args.Target, args.DamageDealt, ent.Comp.Sound, ent.Comp.ForceSound);
    }

    /// <summary>
    /// Create visual effects for the fired hitscan weapon.
    /// </summary>
    /// <param name="fromCoordinates">Location to start the effect.</param>
    /// <param name="distance">Distance of the hitscan shot.</param>
    /// <param name="shotAngle">Angle of the shot.</param>
    /// <param name="hitscanUid">The hitscan entity itself.</param>
    public void FireEffects(EntityCoordinates fromCoordinates, float distance, Angle shotAngle, EntityUid hitscanUid)
    {
        if (distance == 0 || !_visualsQuery.TryComp(hitscanUid, out var vizComp))
            return;

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
            shotAngle -= gridRot;
        }
        else
        {
            shotAngle -= _transform.GetWorldRotation(fromXform);
        }

        if (distance >= 1f)
        {
            if (vizComp.MuzzleFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec().Normalized() / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, shotAngle, vizComp.MuzzleFlash, 1f));
            }

            if (vizComp.TravelFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec() * (distance + 0.5f) / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, shotAngle, vizComp.TravelFlash, distance - 1.5f));
            }
        }

        if (vizComp.ImpactFlash != null)
        {
            var coords = fromCoordinates.Offset(shotAngle.ToVec() * distance);
            var netCoords = GetNetCoordinates(coords);

            sprites.Add((netCoords, shotAngle.FlipPositive(), vizComp.ImpactFlash, 1f));
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
