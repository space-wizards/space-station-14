using Content.Shared._Starlight.MassDriver.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.MassDriver.EntitySystems;

public sealed class SharedMassDriverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming Timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMassDriverComponent, MassDriverComponent>();
        while (query.MoveNext(out var uid, out var activeMassDriver, out var massDriver))
        {
            if (Timing.CurTime < activeMassDriver.NextUpdateTime)
                continue;

            if (activeMassDriver.NextUpdateTime == TimeSpan.Zero)
            {
                activeMassDriver.NextUpdateTime = Timing.CurTime + activeMassDriver.Delay;
                return;
            }
            activeMassDriver.NextUpdateTime = Timing.CurTime + activeMassDriver.Delay;

            var entities = new HashSet<EntityUid>();
            _lookup.GetEntitiesIntersecting(uid, entities);

            if (entities.Count == 0)
                return;

            var xform = Transform(uid);
            var throwing = xform.LocalRotation.ToWorldVec() * (massDriver.CurrentThrowDistance - (massDriver.ThrowCountDelta * (entities.Count - 1)));
            var direction = xform.Coordinates.Offset(throwing);
            var speed = massDriver.CurrentThrowSpeed - (massDriver.ThrowCountDelta * (entities.Count - 1));

            foreach (var entity in entities)
                _throwing.TryThrow(entity, direction, speed);
        }
    }
}