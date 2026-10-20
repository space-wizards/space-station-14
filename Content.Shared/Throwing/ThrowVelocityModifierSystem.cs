using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Throwing;

public sealed partial class ThrowVelocityModifierSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    [SubscribeLocalEvent]
    private void OnThrown(Entity<ThrowVelocityModifierComponent> ent, ref ThrownEvent args)
    {
        if (!_physicsQuery.TryGetComponent(ent, out var physics))
            return;

        _physics.SetLinearVelocity(ent, physics.LinearVelocity * ent.Comp.VelocityMultiplier, body: physics);
    }
}
