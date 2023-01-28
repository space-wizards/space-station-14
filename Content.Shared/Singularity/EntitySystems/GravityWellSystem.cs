using Content.Shared.Ghost;
using Content.Shared.Singularity.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;


namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// Handles <see cref="GravityWellComponent"/>s capturing nearby physics-enabled entities and attracting/repulsing them.
/// </summary>
public sealed partial class SharedGravityWellSystem : VirtualController
{
    #region Dependencies
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IViewVariablesManager _vvManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    #endregion Dependencies

    /// <summary>
    /// The minimum range at which gravscans will act.
    /// Prevents division by zero problems.
    /// </summary>
    public const float MinGravWellRange = 0.00001f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GravityWellComponent, ComponentStartup>(OnGravityWellStartup);

        var vvHandle = _vvManager.GetTypeHandler<GravityWellComponent>();
        vvHandle.AddPath(nameof(GravityWellComponent.TargetScanPeriod), (_, comp) => comp.TargetScanPeriod, SetScanPeriod);
    }

    public override void Shutdown()
    {
        var vvHandle = _vvManager.GetTypeHandler<GravityWellComponent>();
        vvHandle.RemovePath(nameof(GravityWellComponent.TargetScanPeriod));
        base.Shutdown();
    }

    /// <summary>
    /// Updates the set of captured entities for all gravity wells.
    /// </summary>
    public override void Update(float frameTime)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        var curTime = _timing.CurTime;
        foreach(var (gravWell, xform) in EntityManager.EntityQuery<GravityWellComponent, TransformComponent>())
        {
            if (gravWell.NextScanTime <= curTime)
                Update(gravWell.Owner, curTime, gravWell, xform);
        }
    }

    /// <summary>
    /// Updates the set of captured entities for a gravity well.
    /// </summary>
    private void Update(EntityUid uid, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if (Resolve(uid, ref gravWell))
            Update(uid, _timing.CurTime, gravWell, xform);
    }

    /// <summary>
    /// Updates the set of captured entities for a gravity well.
    /// </summary>
    private void Update(EntityUid uid, TimeSpan curTime, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;

        gravWell.LastScanTime = curTime;
        gravWell.NextScanTime = gravWell.LastScanTime + gravWell.TargetScanPeriod;
        gravWell.Captured.Clear();

        if (gravWell.MaxRange < 0.0f || !Resolve(uid, ref xform))
            return;

        foreach(var entity in _lookupSystem.GetEntitiesInRange(xform.MapPosition, gravWell.MaxRange, flags: LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (entity != uid && CanGravPulseAffect(entity))
                gravWell.Captured.Add(entity);
        }
    }

    /// <summary>
    /// Checks whether an entity can be affected by gravity pulses.
    /// TODO: Make this an event or such.
    /// </summary>
    private bool CanGravPulseAffect(EntityUid entity)
    {
        return !( // TODO: Make this an event/tag check/specific immunity component so as to rid ourselves of this code smell.
            EntityManager.HasComponent<SharedGhostComponent>(entity) ||
            EntityManager.HasComponent<MapGridComponent>(entity) ||
            EntityManager.HasComponent<MapComponent>(entity) ||
            EntityManager.HasComponent<GravityWellComponent>(entity)
        );
    }

    #region Physics Controller

    /// <summary>
    /// Updates the gravitational force applied to all objects captured by all gravity wells.
    /// </summary>
    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        foreach(var (gravWell, xform) in EntityManager.EntityQuery<GravityWellComponent, TransformComponent>())
        {
            UpdateBeforeSolve(gravWell.Owner, frameTime, physicsQuery, xformQuery, gravWell, xform);
        }
    }

    /// <summary>
    /// Updates the gravitational force applied to all objects captured by all gravity wells.
    /// </summary>
    public void UpdateBeforeSolve(EntityUid uid, float frameTime, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref gravWell, ref xform))
            return;

        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        UpdateBeforeSolve(uid, frameTime, physicsQuery, xformQuery, gravWell, xform);
    }

    /// <summary>
    /// Updates the gravitational force applied to all objects captured by all gravity wells.
    /// </summary>
    public void UpdateBeforeSolve(
        EntityUid uid,
        float frameTime,
        EntityQuery<PhysicsComponent> physicsQuery, EntityQuery<TransformComponent> xformQuery,
        GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref gravWell, ref xform))
            return;

        var epicenter = xform.MapPosition;
        var minRange2 = MathF.Max(gravWell.MinRange * gravWell.MinRange, MinGravWellRange);
        var maxRange2 = gravWell.MaxRange * gravWell.MaxRange;
        foreach(var entity in gravWell.Captured)
        {
            if(!physicsQuery.TryGetComponent(entity, out var physicsBody)
            || !xformQuery.TryGetComponent(entity, out var entityXform))
                continue;

            if (epicenter.MapId != entityXform.MapID)
                continue;

            var displacement = _xformSystem.GetWorldPosition(entityXform, xformQuery) - epicenter.Position;
            var distance2 = displacement.LengthSquared;
            if (distance2 < minRange2 || distance2 > maxRange2)
                continue;

            // Need ApplyLinearImpulse instead of ApplyForce here because impulse works on KinematicController physicsbodies (ie: humans) and ApplyForce doesn't even though they are on the whitelist.
            var impulse = (gravWell.MatrixAcceleration * displacement) * (frameTime * physicsBody.Mass / distance2);
            _physicsSystem.ApplyLinearImpulse(entity, impulse, body: physicsBody); // TODO: Consider getting a query for FixtureComponents as well. It might speed this up.
        }
    }

    #endregion Physics Controller

    #region Getters/Setters

    /// <summary>
    /// Sets the scan period for a gravity well.
    /// If the new scan period implies that the gravity well was intended to scan already it does so immediately.
    /// </summary>
    public void SetRadialAcceleration(EntityUid uid, float value, GravityWellComponent? gravWell = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;

        if (MathHelper.CloseTo(gravWell.BaseRadialAcceleration, value))
            return;

        gravWell.BaseRadialAcceleration = value;
        UpdateMatrix(uid, gravWell);
    }

    /// <summary>
    /// Sets the scan period for a gravity well.
    /// If the new scan period implies that the gravity well was intended to scan already it does so immediately.
    /// </summary>
    public void SetTangentialAcceleration(EntityUid uid, float value, GravityWellComponent? gravWell = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;

        if (MathHelper.CloseTo(gravWell.BaseTangentialAcceleration, value))
            return;

        gravWell.BaseTangentialAcceleration = value;
        UpdateMatrix(uid, gravWell);
    }

    /// <summary>
    /// Sets the scan period for a gravity well.
    /// If the new scan period implies that the gravity well was intended to scan already it does so immediately.
    /// </summary>
    public void SetScanPeriod(EntityUid uid, TimeSpan value, GravityWellComponent? gravWell = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;

        if (MathHelper.CloseTo(gravWell.TargetScanPeriod.TotalSeconds, value.TotalSeconds))
            return;

        gravWell.TargetScanPeriod = value;
        gravWell.NextScanTime = gravWell.LastScanTime + gravWell.TargetScanPeriod;

        var curTime = _timing.CurTime;
        if (gravWell.NextScanTime <= curTime)
            Update(uid, curTime - gravWell.LastScanTime, gravWell);
    }

    /// <summary>
    /// Updates the matrix acceleration of the gravity well to match the set radial and tangential accelerations.
    /// </summary>
    public void UpdateMatrix(EntityUid uid, GravityWellComponent? gravWell = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;
        
        gravWell.MatrixAcceleration = new(
            -gravWell.BaseRadialAcceleration, -gravWell.BaseTangentialAcceleration, 0.0f,
            +gravWell.BaseTangentialAcceleration, -gravWell.BaseRadialAcceleration, 0.0f,
            0.0f, 0.0f, 1.0f
        );
    }

    #endregion Getters/Setters

    #region Event Handlers

    /// <summary>
    /// Resets the scan timings of the gravity well when the components starts up.
    /// </summary>
    public void OnGravityWellStartup(EntityUid uid, GravityWellComponent comp, ComponentStartup args)
    {
        comp.LastScanTime = _timing.CurTime;
        comp.NextScanTime = comp.LastScanTime + comp.TargetScanPeriod;
        UpdateMatrix(uid, comp);
    }

    #endregion Event Handlers
}
