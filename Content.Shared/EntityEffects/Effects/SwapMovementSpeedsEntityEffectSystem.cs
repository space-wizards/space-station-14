using Content.Shared.Movement.Components;

namespace Content.Shared.EntityEffects.Effects;

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

public sealed partial class SwapMovementSpeeds : EntityEffectBase<SwapMovementSpeeds>;
