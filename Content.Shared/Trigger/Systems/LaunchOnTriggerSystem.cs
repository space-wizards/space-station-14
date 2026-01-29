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

        var linearVelocity = _physics.GetMapLinearVelocity(target);
        // If the linear velocity is length 0, this means it's not moving. Given we want to move it in some direction...
        if (linearVelocity.IsLengthZero())
            // An object that isn't moving is launched in the direction its facing, not the direction it's rotated (objects face away from their rotation).
            linearVelocity = _transform.GetWorldRotation(target).RotateVec(Vector2.UnitY) * -1;

        // When triggered, take the direction the target is moving in (the normalized vector) and multiply it by the speed.
        // Then apply an impulse to the target on the new vector.
        // (If the target is moving NE at 10 m/s, this impulses it NE at speed m/s)
        _physics.ApplyLinearImpulse(target, linearVelocity.Normalized() * ent.Comp.Impulse, body: phys);

        args.Handled = true;
    }
}
