using System.Numerics;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Trigger.Systems;

public sealed class LaunchOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LaunchOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<LaunchOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target is null)
            return;

        if (!TryComp(target, out PhysicsComponent? phys))
            return;

        var linearVelocity = _physics.GetMapLinearVelocity(target.Value);
        // If the linear velocity is length 0, this means it's not moving. Given we want to move it in some direction...
        if (linearVelocity.IsLengthZero())
            // ASSUMPTION: An object at rest is facing [0,1] by default if its radial rotation is 0.0.
            linearVelocity = _transform.GetWorldRotation(target.Value).RotateVec(Vector2.UnitY);

        // When triggered, take the direction the target is moving in (the normalized vector) and multiply it by the speed.
        // Then apply an impulse to the target on the new vector.
        // (If the target is moving NE at 10 m/s, this impulses it NE at speed m/s)
        _physics.ApplyLinearImpulse(target.Value,
            linearVelocity.Normalized() * ent.Comp.Speed,
            body: phys);

        args.Handled = true;
    }
}
