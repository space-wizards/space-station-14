using Content.Server.Atmos.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Ghost;
using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// The server side version of <see cref="SharedGravityWellSystem"/>.
/// Primarily responsible for managing <see cref="GravityWellComponent"/>s.
/// Handles the gravitational pulses they can emit.
/// </summary>
public sealed class GravityWellSystem : SharedGravityWellSystem
{
    #region Dependencies
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IViewVariablesManager _vvManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    #endregion Dependencies

    /// <summary>
    /// The minimum range at which gravpulses will act.
    /// Prevents division by zero problems.
    /// </summary>
    public const float MinGravPulseRange = 0.00001f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GravityWellComponent, ComponentStartup>(OnGravityWellStartup);

        var vvHandle = _vvManager.GetTypeHandler<GravityWellComponent>();
        vvHandle.AddPath(nameof(GravityWellComponent.TargetPulsePeriod), (_, comp) => comp.TargetPulsePeriod, SetPulsePeriod);
    }

    public override void Shutdown()
    {
        var vvHandle = _vvManager.GetTypeHandler<GravityWellComponent>();
        vvHandle.RemovePath(nameof(GravityWellComponent.TargetPulsePeriod));
        base.Shutdown();
    }

    /// <summary>
    /// Updates the pulse cooldowns of all gravity wells.
    /// If they are off cooldown it makes them emit a gravitational pulse and reset their cooldown.
    /// </summary>
    /// <param name="frameTime">The time elapsed since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<GravityWellComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var gravWell, out var xform))
        {
            var curTime = _timing.CurTime;
            if (gravWell.NextPulseTime <= curTime)
                Update(uid, curTime - gravWell.LastPulseTime, gravWell, xform);
        }
    }

    /// <summary>
    /// Makes a gravity well emit a gravitational pulse and puts it on cooldown.
    /// The longer since the last gravitational pulse the more force it applies on affected entities.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to make pulse.</param>
    /// <param name="gravWell">The state of the gravity well to make pulse.</param>
    /// <param name="xform">The transform of the gravity well to make pulse.</param>
    private void Update(EntityUid uid, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if (Resolve(uid, ref gravWell))
            Update(uid, _timing.CurTime - gravWell.LastPulseTime, gravWell, xform);
    }

    /// <summary>
    /// Makes a gravity well emit a gravitational pulse and puts it on cooldown.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to make pulse.</param>
    /// <param name="gravWell">The state of the gravity well to make pulse.</param>
    /// <param name="frameTime">The amount to consider as having passed since the last gravitational pulse by the gravity well. Pulse force scales with this.</param>
    /// <param name="xform">The transform of the gravity well to make pulse.</param>
    private void Update(EntityUid uid, TimeSpan frameTime, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;

        gravWell.LastPulseTime = _timing.CurTime;
        gravWell.NextPulseTime = gravWell.LastPulseTime + gravWell.TargetPulsePeriod;
        if (gravWell.MaxRange < 0.0f || !Resolve(uid, ref xform))
            return;

        var scale = (float)frameTime.TotalSeconds;
        GravPulse(uid, gravWell.MaxRange, gravWell.MinRange, gravWell.BaseRadialAcceleration * scale, gravWell.BaseTangentialAcceleration * scale, xform);
    }

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
        => GravPulse(entityPos.ToMap(EntityManager, _transform), maxRange, minRange, in baseMatrixDeltaV);

    /// <summary>
    /// Greates a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="entityPos">The epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="baseRadialDeltaV">The base radial velocity that will be added to entities within range towards the center of the gravitational pulse.</param>
    /// <param name="baseTangentialDeltaV">The base tangential velocity that will be added to entities within countrclockwise around the center of the gravitational pulse.</param>
    public void GravPulse(EntityCoordinates entityPos, float maxRange, float minRange, float baseRadialDeltaV = 0.0f, float baseTangentialDeltaV = 0.0f)
        => GravPulse(entityPos.ToMap(EntityManager, _transform), maxRange, minRange, baseRadialDeltaV, baseTangentialDeltaV);

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
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach(var entity in _lookup.GetEntitiesInRange(mapPos.MapId, epicenter, maxRange, flags: LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (!bodyQuery.TryGetComponent(entity, out var physics)
                || physics.BodyType == BodyType.Static)
            {
                continue;
            }

            if (TryComp<MovedByPressureComponent>(entity, out var movedPressure) && !movedPressure.Enabled) //Ignore magboots users
                continue;

            if(!CanGravPulseAffect(entity))
                continue;

            var displacement = epicenter - _transform.GetWorldPosition(entity, xformQuery);
            var distance2 = displacement.LengthSquared();
            if (distance2 < minRange2)
                continue;

            var scaling = (1f / distance2) * physics.Mass; // TODO: Variable falloff gradiants.
            _physics.ApplyLinearImpulse(entity, (displacement * baseMatrixDeltaV) * scaling, body: physics);
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

    #region Getters/Setters

    /// <summary>
    /// Sets the pulse period for a gravity well.
    /// If the new pulse period implies that the gravity well was intended to pulse already it does so immediately.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to set the pulse period for.</param>
    /// <param name="value">The new pulse period for the gravity well.</param>
    /// <param name="gravWell">The state of the gravity well to set the pulse period for.</param>
    public void SetPulsePeriod(EntityUid uid, TimeSpan value, GravityWellComponent? gravWell = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;

        if (MathHelper.CloseTo(gravWell.TargetPulsePeriod.TotalSeconds, value.TotalSeconds))
            return;

        gravWell.TargetPulsePeriod = value;
        gravWell.NextPulseTime = gravWell.LastPulseTime + gravWell.TargetPulsePeriod;

        var curTime = _timing.CurTime;
        if (gravWell.NextPulseTime <= curTime)
            Update(uid, curTime - gravWell.LastPulseTime, gravWell);
    }

    #endregion Getters/Setters

    #region Event Handlers

    /// <summary>
    /// Resets the pulse timings of the gravity well when the components starts up.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to start up.</param>
    /// <param name="comp">The state of the gravity well to start up.</param>
    /// <param name="args">The startup prompt arguments.</param>
    public void OnGravityWellStartup(EntityUid uid, GravityWellComponent comp, ComponentStartup args)
    {
        comp.LastPulseTime = _timing.CurTime;
        comp.NextPulseTime = comp.LastPulseTime + comp.TargetPulsePeriod;
    }

    #endregion Event Handlers
}
