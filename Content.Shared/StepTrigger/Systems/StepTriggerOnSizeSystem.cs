using Content.Shared.Physics;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.StepTrigger.Systems;

/// <inheritdoc cref="StepTriggerOnSizeComponent"/>
public sealed class StepTriggerOnSizeSystem : EntitySystem
{
    private EntityQuery<PhysicsComponent> _physicsQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<StepTriggerOnSizeComponent, StepTriggerAttemptEvent>(OnStepTriggerOnSizeAttempt);
    }

    /// <summary>
    /// Called when an entity attempts to trigger a step event on an entity that has a <see cref="StepTriggerOnSizeComponent"/>.
    /// Cancels the event if the triggering entity's collision group doesn't match the allowed mask.
    /// </summary>
    private void OnStepTriggerOnSizeAttempt(Entity<StepTriggerOnSizeComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (!_physicsQuery.TryComp(args.Tripper, out var physics))
            return;

        /// Check if the tripper's collision layer matches any of the allowed <see cref="StepTriggerOnSizeComponent.CollisionMask"/>.
        /// Bitwise AND to determine if any bits overlap.
        if (((CollisionGroup)physics.CollisionLayer & ent.Comp.CollisionMask) == 0)
        {
            args.Continue = true;
            return;
        }

        args.Continue = false;
    }
}
