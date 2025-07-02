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
    public static readonly EntProtoId SlowdownProtoId = "StatusEffectSlowdown";

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectRemovedEvent>(OnMovementModRemoved);
        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshRelay);
    }

    private void OnMovementModRemoved(Entity<MovementModStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        entity.Comp.SprintSpeedModifier = 1f;
        entity.Comp.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnRefreshRelay(Entity<MovementModStatusEffectComponent> entity,
        ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(entity.Comp.WalkSpeedModifier, entity.Comp.WalkSpeedModifier);
    }

    /// <summary>
    /// Modifies a mob's walking/running speed temporarily
    /// </summary>
    public bool TryModMovement(EntityUid uid,
        TimeSpan time,
        float speedModifier,
        bool refresh = true)
    {
        return TryModMovement(uid, time, speedModifier, speedModifier, refresh);
    }

    /// <summary>
    /// Modifies a mob's walking/running speed temporarily
    /// </summary>
    public bool TryModMovement(EntityUid uid,
        TimeSpan time,
        float walkSpeedModifier,
        float sprintSpeedModifier,
        bool refresh = true)
    {
        if (time <= TimeSpan.Zero)
            return false;

        return _status.TryUpdateStatusEffectDuration(uid, SlowdownProtoId, out var status, time, refresh)
               && TryUpdateMovementStatus(uid, status.Value, walkSpeedModifier, sprintSpeedModifier);
    }

    /// <summary>
    /// Updates the status entity's <see cref="MovementModStatusEffectComponent"/> modifiers to the inputted values.
    /// Then refreshes the movement speed of the entity.
    /// </summary>
    /// <param name="uid">Entity whose component we're updating</param>
    /// <param name="status">Status effect entity whose modifiers we are updating</param>
    /// <param name="walkSpeedModifier">New walkSpeedModifer we're applying</param>
    /// <param name="sprintSpeedModifier">New sprintSpeedModifier we're applying</param>
    public bool TryUpdateMovementStatus(EntityUid uid,
        Entity<MovementModStatusEffectComponent?> status,
        float walkSpeedModifier,
        float sprintSpeedModifier)
    {
        if (!Resolve(status, ref status.Comp))
            return false;

        status.Comp.SprintSpeedModifier = sprintSpeedModifier;
        status.Comp.WalkSpeedModifier = walkSpeedModifier;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        return true;
    }

    /// <summary>
    /// An overflow method that takes one speed modifier and applies it to both walk speed and sprint speed.
    /// </summary>
    /// <param name="uid">Entity whose component we're updating</param>
    /// <param name="status">Status effect entity whose modifiers we are updating</param>
    /// <param name="speedModifier">New walkSpeedModifer we're applying</param>
    public bool TryUpdateMovementStatus(EntityUid uid,
        Entity<MovementModStatusEffectComponent?> status,
        float speedModifier)
    {
        return TryUpdateMovementStatus(uid, status, speedModifier, speedModifier);
    }
}
