using Content.Shared.Effects.Components;
using Content.Shared.Effects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Effects;

public sealed partial class ParticleEmitterSystem : SharedParticleEmitterSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const float MovementEpsilon = 0.001f;
    private const float MovementEpsilonSquared = MovementEpsilon * MovementEpsilon;

    [SubscribeLocalEvent]
    private void OnActiveEmitterInit(Entity<ActiveParticleEmitterComponent> ent, ref ComponentInit args)
    {
        var coordinates = _transform.GetMoverCoordinates(ent, Transform(ent));
        InitializeRuntimeState(ent.Comp, coordinates);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<ParticleEmitterComponent, ActiveParticleEmitterComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var emitter, out var active, out var xform))
        {
            if (emitter.EffectPrototype is not { } effectPrototype)
                continue;

            var coordinates = _transform.GetMoverCoordinates(uid, xform);
            if (!coordinates.IsValid(EntityManager))
            {
                InitializeRuntimeState(active, EntityCoordinates.Invalid);
                continue;
            }

            if (!active.LastPosition.IsValid(EntityManager) ||
                !active.LastEmissionPosition.IsValid(EntityManager) ||
                active.LastPosition.EntityId != coordinates.EntityId ||
                active.LastEmissionPosition.EntityId != coordinates.EntityId)
            {
                InitializeRuntimeState(active, coordinates);
                continue;
            }

            var movementDelta = coordinates.Position - active.LastPosition.Position;
            if (movementDelta.LengthSquared() < MovementEpsilonSquared)
                continue;

            active.LastPosition = coordinates;

            var emissionDelta = coordinates.Position - active.LastEmissionPosition.Position;
            var maxSpawnDistanceSquared = emitter.MaxSpawnDistance * emitter.MaxSpawnDistance;
            var maxDistanceReached = emissionDelta.LengthSquared() >= maxSpawnDistanceSquared;

            if (_timing.CurTime < active.NextEmissionTime && !maxDistanceReached)
                continue;

            Spawn(effectPrototype, coordinates);

            active.LastEmissionPosition = coordinates;
            active.NextEmissionTime = _timing.CurTime + TimeSpan.FromSeconds(emitter.SpawnInterval);
        }
    }

    private void InitializeRuntimeState(ActiveParticleEmitterComponent component, EntityCoordinates coordinates)
    {
        component.LastPosition = coordinates;
        component.LastEmissionPosition = coordinates;
        component.NextEmissionTime = _timing.CurTime;
    }
}
