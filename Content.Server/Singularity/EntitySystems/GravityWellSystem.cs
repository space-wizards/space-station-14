using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

using Content.Shared.Singularity.EntitySystems;

using Content.Server.Ghost.Components;
using Content.Server.Singularity.Components;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// The server side version of <see cref="SharedGravityWellSystem"/>.
/// Primarily responsible for managing <see cref="GravityWellComponent"/>s.
/// Handles the gravitational pulses they can emit.
/// </summary>
public sealed class GravityWellSystem : SharedGravityWellSystem
{
#region Dependencies
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
#endregion Dependencies

    /// <summary>
    /// The minimum range at which gravpulses will act.
    /// Prevents division by zero problems.
    /// </summary>
    public const float MinGravPulseRange = 0.00001f;

    /// <summary>
    /// Updates the pulse cooldowns of all gravity wells.
    /// If they are off cooldown it makes them emit a gravitational pulse and reset their cooldown.
    /// </summary>
    /// <param name="frameTime">The time elapsed since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        foreach(var (gravWell, xform) in EntityManager.EntityQuery<GravityWellComponent, TransformComponent>())
        {
            if ((gravWell.TimeSinceLastGravPulse += frameTime) > gravWell.GravPulsePeriod)
                Update(gravWell, xform);
        }
    }

    /// <summary>
    /// Makes a gravity well emit a gravitational pulse and puts it on cooldown.
    /// </summary>
    /// <param name="gravWell">The gravity well to pulse.</param>
    /// <param name="frameTime">The amount to consider as having passed since the last gravitational pulse by the gravity well. Pulse force scales with this.</param>
    /// <param name="xform">The transform of the gravity well entity.</param>
    private void Update(GravityWellComponent gravWell, float frameTime, TransformComponent? xform = null)
    {
        gravWell.TimeSinceLastGravPulse = 0.0f;
        if(!Resolve(gravWell.Owner, ref xform))
            return;
        if (gravWell.MaxRange < 0.0f)
            return;

        GravPulse(xform.MapPosition, gravWell.MaxRange, gravWell.MinRange, gravWell.BaseRadialAcceleration * frameTime, gravWell.BaseTangentialAcceleration * frameTime);
    }

    /// <summary>
    /// Makes a gravity well emit a gravitational pulse and puts it on cooldown.
    /// The longer since the last gravitational pulse the more force it applies on affected entities.
    /// </summary>
    /// <param name="gravWell">The gravity well to make pulse.</param>
    /// <param name="xform">The transform of the gravity well.</param>
    private void Update(GravityWellComponent gravWell, TransformComponent? xform = null)
        => Update(gravWell, gravWell.TimeSinceLastGravPulse, xform);

#region GravPulse

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
        var minRange2 = MathF.Max(minRange * minRange, MinGravPulseRange); // Cache square value for speed. Also apply a sane minimum value to the minimum value so that div/0s don't happen.
        foreach(var entity in _lookup.GetEntitiesInRange(mapPos.MapId, epicenter, maxRange, flags: LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if(!TryComp<PhysicsComponent?>(entity, out var physics)
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

#endregion GravPulse
}
