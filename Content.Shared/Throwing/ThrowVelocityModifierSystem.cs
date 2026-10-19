using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Throwing;

public sealed partial class ThrowVelocityModifierSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;

    [SubscribeLocalEvent]
    private void OnThrown(EntityUid uid, ThrowVelocityModifierComponent comp, ref ThrownEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        _physics.SetLinearVelocity(uid, physics.LinearVelocity * comp.VelocityMultiplier, body: physics);
    }
}
