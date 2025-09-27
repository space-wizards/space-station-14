using Content.Shared.Power.EntitySystems;
using Content.Shared.Physics.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Physics.EntitySystems;

/// <summary>
/// Handles singularity attractors.
/// </summary>
public sealed class RandomWalkAttractorSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    /// <summary>
    /// The minimum range at which the attraction will act.
    /// Prevents division by zero problems.
    /// </summary>
    public const float MinAttractRange = 0.00001f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomWalkAttractorComponent, MapInitEvent>(OnMapInit);
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

        var query = EntityQueryEnumerator<RandomWalkAttractorComponent, TransformComponent>();
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
    private void Update(EntityUid uid, RandomWalkAttractorComponent? attractor = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref attractor, ref xform))
            return;

        if (!_power.IsPowered(uid))
            return;

        attractor.LastPulseTime = _timing.CurTime;

        var mapPos = _transform.ToMapCoordinates(xform.Coordinates);

        if (mapPos == MapCoordinates.Nullspace)
            return;

        AttractRandomWalkers(mapPos, attractor.BaseRange, attractor.Component);

    }

    public void AttractRandomWalkers(MapCoordinates toLocation, float range, string compName)
    {
        if (!Factory.TryGetRegistration(compName, out var compReg))
        {
            Log.Error($"Tried to use invalid component registration for attracting random walker: {compName}");
            return;
        }

        var query = EntityQueryEnumerator<RandomWalkComponent, TransformComponent>();

        while (query.MoveNext(out var other, out var walk, out var otherXform))
        {
            if (!HasComp(other, compReg.Type))
                continue;

            var otherMapPos = _transform.ToMapCoordinates(otherXform.Coordinates);

            if (otherMapPos.MapId != toLocation.MapId)
                continue;

            var biasBy = toLocation.Position - otherMapPos.Position;
            var length = biasBy.Length();
            if (length <= MinAttractRange)
                return;

            biasBy = Vector2.Normalize(biasBy) * (range / length);

            walk.BiasVector += biasBy;
        }
    }


    /// <summary>
    /// Resets the pulse timings of the attractor when the component starts up.
    /// </summary>
    /// <param name="uid">The uid of the attractor to start up.</param>
    /// <param name="comp">The state of the attractor to start up.</param>
    /// <param name="args">The startup prompt arguments.</param>
    private void OnMapInit(Entity<RandomWalkAttractorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastPulseTime = _timing.CurTime;
    }
}
