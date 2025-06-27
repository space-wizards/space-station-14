using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Components;

/// <summary>
/// This handles the slowed status effect
/// </summary>
public sealed class SlowedStatusSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    public static readonly EntProtoId Slowdown = "StatusEffectSlowdown";

    public override void Initialize()
    {
        SubscribeLocalEvent<SlowedDownComponent, ComponentShutdown>(OnSlowRemove);
        SubscribeLocalEvent<SlowedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<SlowdownStatusEffectComponent, StatusEffectAppliedEvent>(OnSlowStatusApplied);
        SubscribeLocalEvent<SlowdownStatusEffectComponent, StatusEffectRemovedEvent>(OnSlowStatusRemoved);
    }

    private void OnSlowRemove(EntityUid uid, SlowedDownComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, SlowedDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid,
        TimeSpan time,
        bool refresh,
        float speedModifier)
    {
        return TrySlowdown(uid, time, refresh, speedModifier, speedModifier);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid,
        TimeSpan time,
        bool refresh,
        float walkSpeedModifier,
        float sprintSpeedModifier)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!_status.TryAddStatusEffect(uid, Slowdown, time, refresh))
            return false;

        if (!_status.TryGetStatusEffect(uid, Slowdown, out var status)
            || !TryComp<SlowdownStatusEffectComponent>(status, out var slowedStatus))
            return false;

        slowedStatus.SprintSpeedModifier = sprintSpeedModifier;
        slowedStatus.WalkSpeedModifier = walkSpeedModifier;

        TryUpdateSlowedStatus(uid);

        return true;
    }

    /// <summary>
    /// Updates the <see cref="SlowedDownComponent"/> speed modifiers and returns true if speed is being modified,
    /// and false if speed isn't being modified.
    /// </summary>
    /// <param name="entity">Entity whose component we're updating</param>
    /// <param name="ignore">Status effect entity we're ignoring for calculating the comp's modifiers.</param>
    public bool TryUpdateSlowedStatus(Entity<SlowedDownComponent?> entity, EntityUid? ignore = null)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return false;

        if (!_status.TryEffectsWithComp<SlowdownStatusEffectComponent>(entity, out var slowEffects))
            return false;

        // If it's not modifying anything then we don't need it
        var modified = false;

        entity.Comp.WalkSpeedModifier = 1f;
        entity.Comp.SprintSpeedModifier = 1f;

        foreach (var effect in slowEffects)
        {
            if (effect == ignore)
                continue;

            modified = true;
            entity.Comp.WalkSpeedModifier *= effect.Comp1.WalkSpeedModifier;
            entity.Comp.SprintSpeedModifier *= effect.Comp1.SprintSpeedModifier;
        }

        _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);

        return modified;
    }

    private void OnSlowStatusApplied(Entity<SlowdownStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<SlowedDownComponent>(args.Target);
    }

    private void OnSlowStatusRemoved(Entity<SlowdownStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (!TryUpdateSlowedStatus(args.Target, entity))
            RemComp<SlowedDownComponent>(args.Target);
    }
}
