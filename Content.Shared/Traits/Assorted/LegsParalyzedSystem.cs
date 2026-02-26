using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;

namespace Content.Shared.Traits.Assorted;

public sealed class LegsParalyzedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegsParalyzedComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, UnbuckledEvent>(OnUnbuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
        SubscribeLocalEvent<LegsParalyzedComponent, UpdateCanMoveEvent>(OnUpdateCanMoveEvent);
        SubscribeLocalEvent<LegsParalyzedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnStartup(EntityUid uid, LegsParalyzedComponent component, ComponentStartup args)
    {
        // TODO: In future probably must be surgery related wound
        component.SpeedModifier = 0.0f;
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnShutdown(EntityUid uid, LegsParalyzedComponent component, ComponentShutdown args)
    {
        _standingSystem.Stand(uid);

        component.SpeedModifier = 1.0f;
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshSpeed(EntityUid uid, LegsParalyzedComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedModifier, component.SpeedModifier);
    }

    private void OnBuckled(EntityUid uid, LegsParalyzedComponent component, ref BuckledEvent args)
    {
        _standingSystem.Stand(uid);
    }

    private void OnUnbuckled(EntityUid uid, LegsParalyzedComponent component, ref UnbuckledEvent args)
    {
        _standingSystem.Down(uid);
    }

    private void OnUpdateCanMoveEvent(EntityUid uid, LegsParalyzedComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnThrowPushbackAttempt(EntityUid uid, LegsParalyzedComponent component, ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }
}
