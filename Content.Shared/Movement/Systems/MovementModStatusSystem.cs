using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This handles the slowed status effect and other movement status effects.
/// <see cref="MovementModStatusEffectComponent"/> holds a modifier for a status effect which is relayed to a mob's
/// TODO: REWRITE THIS
/// All modifiers are multiplicative.
/// </summary>
public sealed class MovementModStatusSystem : EntitySystem
{
    public static readonly EntProtoId VomitingSlowdown = "VomitingSlowdownStatusEffect";
    public static readonly EntProtoId TaserSlowdown = "TaserSlowdownStatusEffect";
    public static readonly EntProtoId FlashSlowdown = "FlashSlowdownStatusEffect";
    public static readonly EntProtoId StatusEffectFriction = "StatusEffectFriction";

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectRemovedEvent>(OnMovementModRemoved);
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshRelay);
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRemovedEvent>(OnFrictionStatusEffectRemoved);
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRelayedEvent<RefreshFrictionModifiersEvent>>(OnRefreshFrictionStatus);
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRelayedEvent<TileFrictionEvent>>(OnRefreshTileFrictionStatus);
    }

    private void OnMovementModRemoved(Entity<MovementModStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        TryUpdateMovementStatus(args.Target, (ent, ent), 1f);
    }

    private void OnFrictionStatusEffectRemoved(Entity<FrictionStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        TrySetFrictionStatus(entity!, 1f, args.Target);
    }

    private void OnRefreshRelay(
        Entity<MovementModStatusEffectComponent> entity,
        ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args
    )
    {
        args.Args.ModifySpeed(entity.Comp.WalkSpeedModifier, entity.Comp.WalkSpeedModifier);
    }

    private void OnRefreshFrictionStatus(Entity<FrictionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshFrictionModifiersEvent> args)
    {
        var ev = args.Args;
        ev.ModifyFriction(ent.Comp.FrictionModifier);
        ev.ModifyAcceleration(ent.Comp.AccelerationModifier);
        args.Args = ev;
    }

    private void OnRefreshTileFrictionStatus(Entity<FrictionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<TileFrictionEvent> args)
    {
        var ev = args.Args;
        ev.Modifier *= ent.Comp.FrictionModifier;
        args.Args = ev;
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

    /// <summary>
    ///     Applies a friction de-buff to the player.
    /// </summary>
    public bool TryFriction(EntityUid uid,
        TimeSpan time,
        bool refresh,
        float friction,
        float acceleration)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (refresh)
        {
            return _status.TryUpdateStatusEffectDuration(uid, StatusEffectFriction, out var status, time)
                   && TrySetFrictionStatus(status.Value, friction, acceleration, uid);
        }
        else
        {
            return _status.TryAddStatusEffectDuration(uid, StatusEffectFriction, out var status, time)
                   && TrySetFrictionStatus(status.Value, friction, acceleration, uid);
        }
    }

    /// <summary>
    /// Sets the friction status modifiers for a status effect.
    /// </summary>
    /// <param name="status">The status effect entity we're modifying.</param>
    /// <param name="friction">The friction modifier we're applying.</param>
    /// <param name="entity">The entity the status effect is attached to that we need to refresh.</param>
    private bool TrySetFrictionStatus(Entity<FrictionStatusEffectComponent?> status, float friction, EntityUid entity)
    {
        return TrySetFrictionStatus(status, friction, friction, entity);
    }

    /// <summary>
    /// Sets the friction status modifiers for a status effect.
    /// </summary>
    /// <param name="status">The status effect entity we're modifying.</param>
    /// <param name="friction">The friction modifier we're applying.</param>
    /// <param name="acceleration">The acceleration modifier we're applying</param>
    /// <param name="entity">The entity the status effect is attached to that we need to refresh.</param>
    private bool TrySetFrictionStatus(Entity<FrictionStatusEffectComponent?> status, float friction, float acceleration, EntityUid entity)
    {
        if (!Resolve(status, ref status.Comp, false))
            return false;

        status.Comp.FrictionModifier = friction;
        status.Comp.AccelerationModifier = acceleration;
        Dirty(status);

        _movementSpeedModifier.RefreshFrictionModifiers(entity);
        return true;
    }
}
