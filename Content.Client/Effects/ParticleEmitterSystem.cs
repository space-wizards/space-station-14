using Content.Shared.Effects.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Effects;

public sealed partial class ParticleEmitterSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<ParticleEmitterComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var emitter, out var xform))
        {
            if (!emitter.Enabled || emitter.EffectPrototype == null)
                continue;

            var inRange = _transform.InRange(xform.Coordinates, emitter.LastCoordinates, emitter.MaxDistance);
            if (inRange && _timing.CurTime < emitter.TargetTime)
                continue;

            emitter.LastCoordinates = _transform.GetMoverCoordinates(xform.Coordinates);
            emitter.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(emitter.Cooldown);

            SpawnParticles(uid, emitter, xform);
        }
    }

    private void SpawnParticles(EntityUid uid, ParticleEmitterComponent component, TransformComponent uidXform)
    {
        // Don't show particles unless the user is moving.
        if (_container.TryGetContainingContainer((uid, uidXform, null), out var container) &&
            TryComp<PhysicsComponent>(container.Owner, out var body) &&
            body.LinearVelocity.LengthSquared() < 1f)
        {
            return;
        }

        var coordinates = uidXform.Coordinates;
        var gridUid = _transform.GetGrid(coordinates);
        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            coordinates = new EntityCoordinates(gridUid.Value, _mapSystem.WorldToLocal(gridUid.Value, grid, _transform.ToMapCoordinates(coordinates).Position));
        }
        else if (uidXform.MapUid != null)
        {
            coordinates = new EntityCoordinates(uidXform.MapUid.Value, _transform.GetWorldPosition(uidXform));
        }
        else
        {
            return;
        }

        Spawn(component.EffectPrototype, coordinates);
    }
}
