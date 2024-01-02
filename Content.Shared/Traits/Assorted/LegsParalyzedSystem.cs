using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;

namespace Content.Shared.Traits.Assorted;

public sealed class LegsParalyzedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegsParalyzedComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<LegsParalyzedComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
        SubscribeLocalEvent<LegsParalyzedComponent, UpdateCanMoveEvent>(OnUpdateCanMoveEvent);
        SubscribeLocalEvent<LegsParalyzedComponent, RefreshMovementSpeedModifiersEvent>(OnUpdateSpeedModifiers);
    }

    private void OnStartup(EntityUid uid, LegsParalyzedComponent component, ComponentStartup args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnShutdown(EntityUid uid, LegsParalyzedComponent component, ComponentShutdown args)
    {
        _standingSystem.Stand(uid);
        _bodySystem.UpdateMovementSpeed(uid);
    }

    private void OnUpdateSpeedModifiers(EntityUid uid, LegsParalyzedComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        // TODO: In future probably must be surgery related wound
        args.ModifySpeed(0, 0); // It's hardcoded, sure, but it's full on leg paralysis.
    }

    private void OnBuckleChange(EntityUid uid, LegsParalyzedComponent component, ref BuckleChangeEvent args)
    {
        if (args.Buckling)
        {
            _standingSystem.Stand(args.BuckledEntity);
        }
        else
        {
            _standingSystem.Down(args.BuckledEntity);
        }
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
