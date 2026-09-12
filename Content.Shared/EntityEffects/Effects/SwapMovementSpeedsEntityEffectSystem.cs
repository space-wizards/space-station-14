using Content.Shared.Movement.Components;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Swaps this entity's base walk and sprint speeds, preserving their modifiers.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class SwapMovementSpeedsEntityEffectSystem : EntityEffectSystem<MetaDataComponent, SwapMovementSpeeds>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<SwapMovementSpeeds> args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(entity);
        (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) =
            (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

        Dirty(entity, movementSpeed);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SwapMovementSpeeds : EntityEffectBase<SwapMovementSpeeds>;
