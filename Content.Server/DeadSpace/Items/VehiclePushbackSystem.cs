using System.Numerics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.DeadSpace.Items;

public sealed class VehiclePushbackSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VehiclePushbackComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var push, out var body))
        {
            _physics.ApplyLinearImpulse(uid, push.ImpulsePerTick, body: body);
            push.TicksLeft--;
            if (push.TicksLeft <= 0)
                RemCompDeferred<VehiclePushbackComponent>(uid);
        }
    }
}
