using Content.Shared.Movement.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This handles the slowed status effect and other movement status effects.
/// <see cref="MovementModStatusEffectComponent"/> holds a modifier for a status effect which is applied to a mob's
/// <see cref="MovementModStatusComponent"/> which is applied to movers to hold a modifier which
/// <see cref="MovementSpeedModifierComponent"/> can handle.
/// All modifiers are multiplicative.
/// </summary>
public sealed class MovementModStatusSystem : EntitySystem
{
    public static readonly EntProtoId SlowdownProtoId = "StatusEffectSlowdown";

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MovementModStatusComponent, ComponentShutdown>(OnSlowRemove);
        SubscribeLocalEvent<MovementModStatusComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectAppliedEvent>(OnSlowStatusApplied);
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectRemovedEvent>(OnSlowStatusRemoved);
    }

    private void OnSlowRemove(EntityUid uid, MovementModStatusComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, MovementModStatusComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    /// <summary>
    /// Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid,
        TimeSpan time,
        float speedModifier,
        bool refresh = true)
    {
        return TrySlowdown(uid, time, speedModifier, speedModifier, refresh);
    }

    /// <summary>
    /// Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid,
        TimeSpan time,
        float walkSpeedModifier,
        float sprintSpeedModifier,
        bool refresh = true)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!_status.TryAddStatusEffect(uid, SlowdownProtoId, out var status, time, refresh))
            return false;

        if (status == null)
            return false;

        return TryUpdateMovementStatus(uid, status.Value, walkSpeedModifier, sprintSpeedModifier);
    }

    /// <summary>
    /// An overload of TryUpdateSlowedStatus that first updates the
    /// <see cref="MovementModStatusEffectComponent"/> modifiers to the inputted values.
    /// This is typically run at the start of a status effect's life.
    /// </summary>
    /// <param name="entity">Entity whose component we're updating</param>
    /// <param name="status">Status effect entity whose modifiers we are updating</param>
    /// <param name="walkSpeedModifier">New walkSpeedModifer we're applying</param>
    /// <param name="sprintSpeedModifier">New sprintSpeedModifier we're applying</param>
    public bool TryUpdateMovementStatus(Entity<MovementModStatusComponent?> entity,
        Entity<MovementModStatusEffectComponent?> status,
        float walkSpeedModifier,
        float sprintSpeedModifier)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false) || !Resolve(status, ref status.Comp))
            return false;

        status.Comp.SprintSpeedModifier = sprintSpeedModifier;
        status.Comp.WalkSpeedModifier = walkSpeedModifier;

        return TryUpdateMovementStatus(entity);
    }

    /// <summary>
    /// Updates the <see cref="MovementModStatusComponent"/> speed modifiers and returns true if speed is being modified,
    /// and false if speed isn't being modified.
    /// </summary>
    /// <param name="entity">Entity whose component we're updating</param>
    /// <param name="ignore">Status effect entity we're ignoring for calculating the comp's modifiers.</param>
    public bool TryUpdateMovementStatus(Entity<MovementModStatusComponent?> entity, EntityUid? ignore = null)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return false;

        if (!_status.TryEffectsWithComp<MovementModStatusEffectComponent>(entity, out var slowEffects))
            return false;

        // If it's not modifying anything then we don't need it
        var modified = false;
        var movementMod = entity.Comp;

        movementMod.WalkSpeedModifier = 1f;
        movementMod.SprintSpeedModifier = 1f;

        foreach (var effect in slowEffects)
        {
            if (effect == ignore)
                continue;

            modified = true;
            movementMod.WalkSpeedModifier *= effect.Comp1.WalkSpeedModifier;
            movementMod.SprintSpeedModifier *= effect.Comp1.SprintSpeedModifier;
        }

        _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);

        return modified;
    }

    private void OnSlowStatusApplied(Entity<MovementModStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<MovementModStatusComponent>(args.Target);
    }

    private void OnSlowStatusRemoved(Entity<MovementModStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (!TryUpdateMovementStatus(args.Target, entity))
            RemComp<MovementModStatusComponent>(args.Target);
    }
}
