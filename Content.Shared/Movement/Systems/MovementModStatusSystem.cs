using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class MovementModStatusSystem : EntitySystem
{
    public static readonly EntProtoId StatusEffectFriciton = "StatusEffectFriction";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectAppliedEvent>(OnFrictionStatusEffectApplied);
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRemovedEvent>(OnFrictionStatusEffectRemoved);

        SubscribeLocalEvent<FrictionStatusModifierComponent, ComponentShutdown>(OnFrictionRemove);
        SubscribeLocalEvent<FrictionStatusModifierComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionStatus);
        SubscribeLocalEvent<FrictionStatusModifierComponent, TileFrictionEvent>(OnRefreshTileFrictionStatus);
    }

    private void OnFrictionRemove(Entity<FrictionStatusModifierComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.FrictionModifier = 1f;
        ent.Comp.AccelerationModifier = 1f;
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
    }

    private void OnRefreshFrictionStatus(Entity<FrictionStatusModifierComponent> ent, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(ent.Comp.FrictionModifier);
        args.ModifyAcceleration(ent.Comp.AccelerationModifier);
    }

    private void OnRefreshTileFrictionStatus(Entity<FrictionStatusModifierComponent> ent, ref TileFrictionEvent args)
    {
        args.Modifier *= ent.Comp.FrictionModifier;
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
            return _status.TryUpdateStatusEffectDuration(uid, StatusEffectFriciton, out var status, time)
                   && TrySetFrictionStatus(status.Value, friction, acceleration, uid);
        }
        else
        {
            return _status.TryAddStatusEffectDuration(uid, StatusEffectFriciton, out var status, time)
                   && TrySetFrictionStatus(status.Value, friction, acceleration, uid);
        }
    }

    /// <summary>
    ///
    /// </summary>
    private bool TrySetFrictionStatus(Entity<FrictionStatusEffectComponent?> status, float friction, float acceleration, EntityUid entity)
    {
        if (!Resolve(status, ref status.Comp, false))
            return false;

        status.Comp.FrictionModifier = friction;
        status.Comp.AccelerationModifier = acceleration;

        return TryUpdateFrictionStatus(entity);
    }

    /// <summary>
    ///     Tries to update the friction modifiers on the friction de-buff, returns true if it was able to apply modifiers.
    /// </summary>
    private bool TryUpdateFrictionStatus(Entity<FrictionStatusModifierComponent?> entity, EntityUid? ignore = null)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return false;

        if (!_status.TryEffectsWithComp<FrictionStatusEffectComponent>(entity, out var frictionEffects))
            return false;

        var modified = false;

        entity.Comp.FrictionModifier = 1f;
        entity.Comp.AccelerationModifier = 1f;

        foreach (var effect in frictionEffects)
        {
            if (effect.Owner == ignore)
                continue;

            modified = true;
            entity.Comp.FrictionModifier *= effect.Comp1.FrictionModifier;
            entity.Comp.AccelerationModifier *= effect.Comp1.AccelerationModifier;
        }

        Dirty(entity);
        _movementSpeedModifier.RefreshFrictionModifiers(entity);

        return modified;
    }

    private void OnFrictionStatusEffectApplied(Entity<FrictionStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        EnsureComp<FrictionStatusModifierComponent>(args.Target);
        // This does not update Friction Modifiers since they may not have been initialized for the status effect yet. You'll need to do that yourself.
    }

    private void OnFrictionStatusEffectRemoved(Entity<FrictionStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        // Set modifiers to 1 so that they don't mistakenly get applied when the component refreshes
        if (!TryUpdateFrictionStatus(args.Target, entity))
            RemComp<FrictionStatusModifierComponent>(args.Target);
    }
}
