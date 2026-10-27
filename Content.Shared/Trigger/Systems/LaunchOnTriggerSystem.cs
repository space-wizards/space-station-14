using System.Numerics;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Trigger.Systems;

public sealed class LaunchOnTriggerSystem : XOnTriggerSystem<LaunchOnTriggerComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    protected override void OnTrigger(Entity<LaunchOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp(target, out PhysicsComponent? phys))
            return;

        var directionSource = ent.Comp.UseTargetDirection ? target : ent.Owner;
        var linearVelocity = _physics.GetMapLinearVelocity(directionSource);
        if (linearVelocity.IsLengthZero())
            // Stationary sources use their facing direction, opposite their rotation.
            linearVelocity = _transform.GetWorldRotation(directionSource).RotateVec(Vector2.UnitY) * -1;

        _physics.ApplyLinearImpulse(target, linearVelocity.Normalized() * ent.Comp.Impulse, body: phys);

        args.Handled = true;
    }
}
