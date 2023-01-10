using Content.Server.Ghost.Components;
using Content.Server.Singularity.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Singularity.EntitySystems;

public sealed partial class GravityWellSystem
{
    /// <summary>
    /// Checks whether an entity can be affected by gravity pulses.
    /// TODO: Make this an event or such.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    private bool CanGravPulseAffect(EntityUid entity)
    {
        return !(
            EntityManager.HasComponent<GhostComponent>(entity) ||
            EntityManager.HasComponent<MapGridComponent>(entity) ||
            EntityManager.HasComponent<MapComponent>(entity) ||
            EntityManager.HasComponent<GravityWellComponent>(entity)
        );
    }

    /// <summary>
    /// Greates a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="uid">The entity at the epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="baseMatrixDeltaV">The base velocity added to any entities within affected by the gravity pulse scaled by the displacement of those entities from the epicenter.</param>
    /// <param name="xform">(optional) The transform of the entity at the epicenter of the gravitational pulse.</param>
    public void GravPulse(EntityUid uid, float maxRange, float minRange, in Matrix3 baseMatrixDeltaV, TransformComponent? xform = null)
    {
        if (Resolve(uid, ref xform))
            GravPulse(xform.Coordinates, maxRange, minRange, in baseMatrixDeltaV);
    }

    /// <summary>
    /// Greates a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="uid">The entity at the epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="baseRadialDeltaV">The base radial velocity that will be added to entities within range towards the center of the gravitational pulse.</param>
    /// <param name="baseTangentialDeltaV">The base tangential velocity that will be added to entities within countrclockwise around the center of the gravitational pulse.</param>
    /// <param name="xform">(optional) The transform of the entity at the epicenter of the gravitational pulse.</param>
    public void GravPulse(EntityUid uid, float maxRange, float minRange, float baseRadialDeltaV = 0.0f, float baseTangentialDeltaV = 0.0f, TransformComponent? xform = null)
    {
        if (Resolve(uid, ref xform))
            GravPulse(xform.Coordinates, maxRange, minRange, baseRadialDeltaV, baseTangentialDeltaV);
    }

    /// <summary>
    /// Greates a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="entityPos">The epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="baseMatrixDeltaV">The base velocity added to any entities within affected by the gravity pulse scaled by the displacement of those entities from the epicenter.</param>
    public void GravPulse(EntityCoordinates entityPos, float maxRange, float minRange, in Matrix3 baseMatrixDeltaV)
        => GravPulse(entityPos.ToMap(EntityManager), maxRange, minRange, in baseMatrixDeltaV);

    /// <summary>
    /// Greates a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="entityPos">The epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="baseRadialDeltaV">The base radial velocity that will be added to entities within range towards the center of the gravitational pulse.</param>
    /// <param name="baseTangentialDeltaV">The base tangential velocity that will be added to entities within countrclockwise around the center of the gravitational pulse.</param>
    public void GravPulse(EntityCoordinates entityPos, float maxRange, float minRange, float baseRadialDeltaV = 0.0f, float baseTangentialDeltaV = 0.0f)
        => GravPulse(entityPos.ToMap(EntityManager), maxRange, minRange, baseRadialDeltaV, baseTangentialDeltaV);

    /// <summary>
    /// Causes a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="mapPos">The epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse. Exists to prevent div/0 errors.</param>
    /// <param name="baseMatrixDeltaV">The base velocity added to any entities within affected by the gravity pulse scaled by the displacement of those entities from the epicenter.</param>
    public void GravPulse(MapCoordinates mapPos, float maxRange, float minRange, in Matrix3 baseMatrixDeltaV)
    {
        if (mapPos == MapCoordinates.Nullspace)
            return; // No gravpulses in nullspace please.

        var epicenter = mapPos.Position;
        var minRange2 = MathF.Max(minRange * minRange, MinGravWellRange); // Cache square value for speed. Also apply a sane minimum value to the minimum value so that div/0s don't happen.
        foreach(var entity in _lookup.GetEntitiesInRange(mapPos.MapId, epicenter, maxRange, flags: LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if(!TryComp<PhysicsComponent>(entity, out var physics)
            || physics.BodyType == BodyType.Static)
                continue;

            if(!CanGravPulseAffect(entity))
                continue;

            var displacement = epicenter - Transform(entity).WorldPosition;
            var distance2 = displacement.LengthSquared;
            if (distance2 < minRange2)
                continue;

            var scaling = (1f / distance2) * physics.Mass; // TODO: Variable falloff gradiants.
            _physics.ApplyLinearImpulse(physics, (displacement * baseMatrixDeltaV) * scaling);
        }
    }

    /// <summary>
    /// Causes a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="mapPos">The epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse. Exists to prevent div/0 errors.</param>
    /// <param name="baseRadialDeltaV">The base amount of velocity that will be added to entities in range towards the epicenter of the pulse.</param>
    /// <param name="baseTangentialDeltaV">The base amount of velocity that will be added to entities in range counterclockwise relative to the epicenter of the pulse.</param>
    public void GravPulse(MapCoordinates mapPos, float maxRange, float minRange = 0.0f, float baseRadialDeltaV = 0.0f, float baseTangentialDeltaV = 0.0f)
        => GravPulse(mapPos, maxRange, minRange, new Matrix3(
            baseRadialDeltaV, +baseTangentialDeltaV, 0.0f,
            -baseTangentialDeltaV, baseRadialDeltaV, 0.0f,
            0.0f, 0.0f, 1.0f
        ));
}
