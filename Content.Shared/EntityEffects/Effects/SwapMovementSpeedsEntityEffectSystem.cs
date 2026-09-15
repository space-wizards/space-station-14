using Content.Shared.Movement.Components;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Swaps this entity's base walk and sprint speeds, preserving their modifiers.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class SwapMovementSpeedsEntityEffectSystem : EntityEffectSystem<MovementSpeedModifierComponent, SwapMovementSpeeds>
{
    protected override void Effect(Entity<MovementSpeedModifierComponent> entity, ref EntityEffectEvent<SwapMovementSpeeds> args)
    {
        (entity.Comp.BaseSprintSpeed, entity.Comp.BaseWalkSpeed) =
            (entity.Comp.BaseWalkSpeed, entity.Comp.BaseSprintSpeed);

        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SwapMovementSpeeds : EntityEffectBase<SwapMovementSpeeds>;
