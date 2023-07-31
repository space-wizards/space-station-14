using System.Numerics;
using Content.Server.Physics.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;

using Content.Server.Singularity.Components;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// Handles singularity attractors.
/// </summary>
public sealed class SingularityAttractorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IViewVariablesManager _vvManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// The minimum range at which the attraction will act.
    /// Prevents division by zero problems.
    /// </summary>
    public const float MinAttractRange = 0.00001f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SingularityAttractorComponent, ComponentStartup>(OnAttractorStartup);
    }

    /// <summary>
    /// Updates the pulse cooldowns of all singularity attractors.
    /// If they are off cooldown it makes them emit an attraction pulse and reset their cooldown.
    /// </summary>
    /// <param name="frameTime">The time elapsed since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        foreach(var (attractor, xform) in EntityManager.EntityQuery<SingularityAttractorComponent, TransformComponent>())
        {
            var curTime = _timing.CurTime;
            if (attractor.NextPulseTime <= curTime)
                Update(attractor.Owner, attractor, xform);
        }
    }

    /// <summary>
    /// Makes an attractor attract all singularities and puts it on cooldown.
    /// </summary>
    /// <param name="uid">The uid of the attractor to make pulse.</param>
    /// <param name="attractor">The state of the attractor to make pulse.</param>
    /// <param name="xform">The transform of the attractor to make pulse.</param>
    private void Update(EntityUid uid, SingularityAttractorComponent? attractor = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref attractor))
            return;

        attractor.LastPulseTime = _timing.CurTime;
        attractor.NextPulseTime = attractor.LastPulseTime + attractor.TargetPulsePeriod;
        if (!Resolve(uid, ref xform))
            return;

        var mapPos = xform.Coordinates.ToMap(EntityManager);

        if (mapPos == MapCoordinates.Nullspace)
            return;

        foreach(var (singulo, walk, singuloXform) in EntityManager.EntityQuery<SingularityComponent, RandomWalkComponent, TransformComponent>())
        {
            var singuloMapPos = singuloXform.Coordinates.ToMap(EntityManager);

            if (singuloMapPos.MapId != mapPos.MapId)
                continue;

            var biasBy = mapPos.Position - singuloMapPos.Position;
            if (biasBy.Length() <= MinAttractRange)
                return;
            biasBy = Vector2.Normalize(biasBy) * (attractor.BaseRange / biasBy.Length());

            walk.BiasVector += biasBy;
        }
    }

    /// <summary>
    /// Resets the pulse timings of the attractor when the component starts up.
    /// </summary>
    /// <param name="uid">The uid of the attractor to start up.</param>
    /// <param name="comp">The state of the attractor to start up.</param>
    /// <param name="args">The startup prompt arguments.</param>
    private void OnAttractorStartup(EntityUid uid, SingularityAttractorComponent comp, ComponentStartup args)
    {
        comp.LastPulseTime = _timing.CurTime;
        comp.NextPulseTime = comp.LastPulseTime + comp.TargetPulsePeriod;
    }
}
