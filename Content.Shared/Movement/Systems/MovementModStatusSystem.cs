using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This handles the application of movement and friction modifiers to an entity as status effects.
/// </summary>
public sealed class MovementModStatusSystem : EntitySystem
{
    public static readonly EntProtoId StatusEffectFriction = "StatusEffectFriction";

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRemovedEvent>(OnFrictionStatusEffectRemoved);
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRelayedEvent<RefreshFrictionModifiersEvent>>(OnRefreshFrictionStatus);
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRelayedEvent<TileFrictionEvent>>(OnRefreshTileFrictionStatus);
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

    private void OnFrictionStatusEffectRemoved(Entity<FrictionStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        TrySetFrictionStatus(entity!, 1f, args.Target);
    }
}
