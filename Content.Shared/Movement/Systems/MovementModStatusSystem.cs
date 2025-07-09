using Content.Shared.Movement.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This handles the slowed status effect and other movement status effects.
/// <see cref="MovementModStatusEffectComponent"/> holds a modifier for a status effect which is relayed to a mob's
/// <see cref="MovementSpeedModifierComponent"/> can handle.
/// All modifiers are multiplicative.
/// </summary>
public sealed class MovementModStatusSystem : EntitySystem
{
    public static readonly EntProtoId VomitingSlowdown = "VomitingSlowdownStatusEffect";
    public static readonly EntProtoId TaserSlowdown = "TaserSlowdownStatusEffect";
    public static readonly EntProtoId FlashSlowdown = "FlashSlowdownStatusEffect";

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectRemovedEvent>(OnMovementModRemoved);
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshRelay);
    }

    private void OnMovementModRemoved(Entity<MovementModStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        TryUpdateMovementStatus(args.Target, (ent, ent), 1f);
    }

    private void OnRefreshRelay(
        Entity<MovementModStatusEffectComponent> entity,
        ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args
    )
    {
        args.Args.ModifySpeed(entity.Comp.WalkSpeedModifier, entity.Comp.WalkSpeedModifier);
    }

    /// <summary>
    /// Modifies mob's walking/running speed temporarily.
    /// </summary>
    /// <param name="uid">Target entity, for which speed should be modified.</param>
    /// <param name="duration">Duration of speed modifying effect.</param>
    /// <param name="speedModifier">
    /// Multiplier by which speed should be modified.
    /// Will be applied to both walking and running speed.
    /// </param>
    /// <param name="refresh">
    /// Should duration be always set to <see cref="duration"/>,
    /// or should be set to longest duration between current effect duration and desired.
    /// </param>
    /// <returns>True if entity have slowdown effect (applied now or previously and duration was modified).</returns>
    public bool TryAddMovementSpeedModDuration(
        EntityUid uid,
        EntProtoId movSpeedSlot,
        TimeSpan duration,
        float speedModifier
    )
    {
        return TryAddMovementSpeedModDuration(uid, movSpeedSlot, duration, speedModifier, speedModifier);
    }

    public bool TryUpdateMovementSpeedModDuration(
        EntityUid uid,
        EntProtoId movSpeedSlot,
        TimeSpan duration,
        float speedModifier
    )
    {
        return TryUpdateMovementSpeedModDuration(uid, movSpeedSlot, duration, speedModifier, speedModifier);
    }


    public bool TryAddMovementSpeedModDuration(
        EntityUid uid,
        EntProtoId movSpeedSlot,
        TimeSpan duration,
        float walkSpeedModifier,
        float sprintSpeedModifier
    )
    {
        return _status.TryAddStatusEffectDuration(uid, movSpeedSlot, out var status, duration)
               && TryUpdateMovementStatus(uid, status!.Value, walkSpeedModifier, sprintSpeedModifier);
    }

    public bool TryUpdateMovementSpeedModDuration(
        EntityUid uid,
        EntProtoId movSpeedSlot,
        TimeSpan? duration,
        float walkSpeedModifier,
        float sprintSpeedModifier
    )
    {
        return _status.TryUpdateStatusEffectDuration(uid, movSpeedSlot, out var status, duration)
               && TryUpdateMovementStatus(uid, status!.Value, walkSpeedModifier, sprintSpeedModifier);
    }

    /// <summary>
    /// Updates entity's movement speed using <see cref="MovementModStatusEffectComponent"/> to provided values.
    /// Then refreshes the movement speed of the entity.
    /// </summary>
    /// <param name="uid">Entity whose component we're updating</param>
    /// <param name="status">Status effect entity whose modifiers we are updating</param>
    /// <param name="walkSpeedModifier">New walkSpeedModifer we're applying</param>
    /// <param name="sprintSpeedModifier">New sprintSpeedModifier we're applying</param>
    public bool TryUpdateMovementStatus(
        EntityUid uid,
        Entity<MovementModStatusEffectComponent?> status,
        float walkSpeedModifier,
        float sprintSpeedModifier
    )
    {
        if (!Resolve(status, ref status.Comp))
            return false;

        status.Comp.SprintSpeedModifier = sprintSpeedModifier;
        status.Comp.WalkSpeedModifier = walkSpeedModifier;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        return true;
    }

    /// <summary>
    /// Updates entity's movement speed using <see cref="MovementModStatusEffectComponent"/> to provided value.
    /// Then refreshes the movement speed of the entity.
    /// </summary>
    /// <param name="uid">Entity whose component we're updating</param>
    /// <param name="status">Status effect entity whose modifiers we are updating</param>
    /// <param name="speedModifier">
    /// Multiplier by which speed should be modified.
    /// Will be applied to both walking and running speed.
    /// </param>
    public bool TryUpdateMovementStatus(
        EntityUid uid,
        Entity<MovementModStatusEffectComponent?> status,
        float speedModifier
    )
    {
        return TryUpdateMovementStatus(uid, status, speedModifier, speedModifier);
    }
}
