using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

using Content.Shared.Singularity.EntitySystems;

using Content.Server.Singularity.Components;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// The server side version of <see cref="SharedGravityWellSystem"/>.
/// Primarily responsible for managing <see cref="GravityWellComponent"/>s.
/// Handles the gravitational scans they can emit.
/// </summary>
public sealed partial class GravityWellSystem : VirtualController
{
#region Dependencies
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IViewVariablesManager _vvManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
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
    /// <param name="frameTime">The time elapsed since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        foreach(var (gravWell, xform) in EntityManager.EntityQuery<GravityWellComponent, TransformComponent>())
        {
            var curTime = _timing.CurTime;
            if (gravWell.NextScanTime <= curTime)
                Update(gravWell.Owner, curTime - gravWell.LastScanTime, gravWell, xform);
        }
    }

    /// <summary>
    /// Updates the set of captured entities for a gravity well.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to make scan.</param>
    /// <param name="gravWell">The state of the gravity well to make scan.</param>
    /// <param name="xform">The transform of the gravity well to make scan.</param>
    private void Update(EntityUid uid, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if (Resolve(uid, ref gravWell))
            Update(uid, _timing.CurTime - gravWell.LastScanTime, gravWell, xform);
    }

    /// <summary>
    /// Updates the set of captured entities for a gravity well.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to make scan.</param>
    /// <param name="gravWell">The state of the gravity well to make scan.</param>
    /// <param name="frameTime">The amount to consider as having passed since the last gravitational scan by the gravity well. Scan force scales with this.</param>
    /// <param name="xform">The transform of the gravity well to make scan.</param>
    private void Update(EntityUid uid, TimeSpan frameTime, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref gravWell))
            return;

        gravWell.LastScanTime = _timing.CurTime;
        gravWell.NextScanTime = gravWell.LastScanTime + gravWell.TargetScanPeriod;
        gravWell.Captured.Clear();

        if (gravWell.MaxRange < 0.0f || !Resolve(uid, ref xform))
            return;

        foreach(var entity in _lookup.GetEntitiesInRange(xform.MapPosition, gravWell.MaxRange, flags: LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if(!TryComp<PhysicsComponent?>(entity, out var physics)
            || physics.BodyType == BodyType.Static)
                continue;

            if(!CanGravPulseAffect(entity))
                continue;
            
            gravWell.Captured.Add(entity);
        }
    }

    #region Physics Controller

    /// <summary>
    /// Updates the gravitational force applied to all objects captured by all gravity wells.
    /// </summary>
    /// <param name="prediction">Whether we are on the client and the client is running prediction.</param>
    /// <param name="frameTime">The amount of time that has passed since the previous physics frame.</param>
    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        foreach(var (gravWell, xform) in EntityManager.EntityQuery<GravityWellComponent, TransformComponent>())
        {
            UpdateBeforeSolve(gravWell.Owner, physicsQuery, xformQuery, gravWell, xform);
        }
    }

    /// <summary>
    /// Updates the gravitational force applied to all objects captured by all gravity wells.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to update.</param>
    /// <param name="gravWell">The state of the gravity well to update.</param>
    /// <param name="xform">The position state of the gravity well.</param>
    public void UpdateBeforeSolve(EntityUid uid, GravityWellComponent? gravWell = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref gravWell, ref xform))
            return;
        
        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        UpdateBeforeSolve(uid, physicsQuery, xformQuery, gravWell, xform);
    }

    /// <summary>
    /// Updates the gravitational force applied to all objects captured by all gravity wells.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to update.</param>
    /// <param name="physicsQuery">A query for the physics body states of affected entities.</param>
    /// <param name="xformQuery">A query for the position states of affected entities.</param>
    /// <param name="gravWell">The state of the gravity well to update.</param>
    /// <param name="xform">The position state of the gravity well.</param>
    public void UpdateBeforeSolve(
        EntityUid uid,
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
                    
            var position = entityXform.MapPosition;
            if (epicenter.MapId != position.MapId)
                continue;
            
            var displacement = position.Position - epicenter.Position;
            var distance2 = displacement.LengthSquared;
            if (distance2 < minRange2 || distance2 > maxRange2)
                continue;
            
            var force = (gravWell.MatrixAcceleration * displacement) * (physicsBody.Mass / distance2);
            _physics.ApplyForce(physicsBody, force);
        }
    }

    #endregion Physics Controller

    #region Getters/Setters

    /// <summary>
    /// Sets the scan period for a gravity well.
    /// If the new scan period implies that the gravity well was intended to scan already it does so immediately.
    /// </summary>
    /// <param name="uid">The uid of the gravity well to set the scan period for.</param>
    /// <param name="value">The new scan period for the gravity well.</param>
    /// <param name="gravWell">The state of the gravity well to set the scan period for.</param>
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
    /// <param name="uid">The uid of the gravity well to set the scan period for.</param>
    /// <param name="value">The new scan period for the gravity well.</param>
    /// <param name="gravWell">The state of the gravity well to set the scan period for.</param>
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
    /// <param name="uid">The uid of the gravity well to set the scan period for.</param>
    /// <param name="value">The new scan period for the gravity well.</param>
    /// <param name="gravWell">The state of the gravity well to set the scan period for.</param>
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
    /// <param name="uid">The uid of the gravity well to update the matrix for.</param>
    /// <param name="gravWell">The state of the gravity well to update the matrix for.</param>
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
    /// <param name="uid">The uid of the gravity well to start up.</param>
    /// <param name="comp">The state of the gravity well to start up.</param>
    /// <param name="args">The startup prompt arguments.</param>
    public void OnGravityWellStartup(EntityUid uid, GravityWellComponent comp, ComponentStartup args)
    {
        comp.LastScanTime = _timing.CurTime;
        comp.NextScanTime = comp.LastScanTime + comp.TargetScanPeriod;
        UpdateMatrix(uid, comp);
    }

    #endregion Event Handlers
}
