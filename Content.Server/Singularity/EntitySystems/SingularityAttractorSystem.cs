using Content.Server.Physics.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// Handles singularity attractors.
/// </summary>
public sealed class SingularityAttractorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// The minimum range at which the attraction will act.
    /// Prevents division by zero problems.
    /// </summary>
    public const float MinAttractRange = 0.00001f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SingularityAttractorComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Updates the pulse cooldowns of all singularity attractors.
    /// If they are off cooldown it makes them emit an attraction pulse and reset their cooldown.
    /// </summary>
    /// <param name="frameTime">The time elapsed since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<SingularityAttractorComponent, TransformComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var attractor, out var xform))
        {
            if (attractor.LastPulseTime + attractor.TargetPulsePeriod <= now)
                Update(uid, attractor, xform);
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
        if (!Resolve(uid, ref attractor, ref xform))
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        attractor.LastPulseTime = _timing.CurTime;

        var mapPos = xform.Coordinates.ToMap(EntityManager, _transform);

        if (mapPos == MapCoordinates.Nullspace)
            return;

        var query = EntityQuery<SingularityComponent, RandomWalkComponent, TransformComponent>();
        foreach (var (singulo, walk, singuloXform) in query)
        {
            var singuloMapPos = singuloXform.Coordinates.ToMap(EntityManager, _transform);

            if (singuloMapPos.MapId != mapPos.MapId)
                continue;

            var biasBy = mapPos.Position - singuloMapPos.Position;
            var length = biasBy.Length();
            if (length <= MinAttractRange)
                return;

            biasBy = Vector2.Normalize(biasBy) * (attractor.BaseRange / length);

            walk.BiasVector += biasBy;
        }
    }

    /// <summary>
    /// Resets the pulse timings of the attractor when the component starts up.
    /// </summary>
    /// <param name="uid">The uid of the attractor to start up.</param>
    /// <param name="comp">The state of the attractor to start up.</param>
    /// <param name="args">The startup prompt arguments.</param>
    private void OnMapInit(Entity<SingularityAttractorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastPulseTime = _timing.CurTime;
    }
}
