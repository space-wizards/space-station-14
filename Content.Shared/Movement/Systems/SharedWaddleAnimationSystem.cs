using Content.Shared.Buckle.Components;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

public abstract class SharedWaddleAnimationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        // Startup
        SubscribeLocalEvent<WaddleAnimationComponent, ComponentStartup>(OnComponentStartup);

        // Start moving possibilities
        SubscribeLocalEvent<WaddleAnimationComponent, MoveInputEvent>(OnMovementInput);
        SubscribeLocalEvent<WaddleAnimationComponent, StoodEvent>((uid, component, args) => OnStood(uid, component, args));

        // Stop moving possibilities
        SubscribeLocalEvent<WaddleAnimationComponent, StunnedEvent>((uid, component, _) => StopWaddling(uid, component));
        SubscribeLocalEvent<WaddleAnimationComponent, DownedEvent>((uid, component, _) => StopWaddling(uid, component));
        SubscribeLocalEvent<WaddleAnimationComponent, BuckleChangeEvent>((uid, component, _) => StopWaddling(uid, component));
        SubscribeLocalEvent<WaddleAnimationComponent, GravityChangedEvent>((uid, component, args) =>
        {
            if (!args.HasGravity && component.IsCurrentlyWaddling)
                StopWaddling(uid, component);
        });
    }

    private void OnComponentStartup(EntityUid uid, WaddleAnimationComponent component, ComponentStartup args)
    {
        if (!TryComp<InputMoverComponent>(uid, out var moverComponent))
            return;

        // If the waddler is currently moving, make them start waddling
        if ((moverComponent.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.AnyDirection)
        {
            RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(uid)));
        }
    }

    private void OnMovementInput(EntityUid uid, WaddleAnimationComponent component, MoveInputEvent args)
    {
        // Prediction mitigation. Prediction means that MoveInputEvents are spammed repeatedly, even though you'd assume
        // they're once-only for the user actually doing something. As such do nothing if we're just repeating this FoR.
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (!args.HasDirectionalMovement && component.IsCurrentlyWaddling)
        {
            StopWaddling(uid, component);

            return;
        }

        // Only start waddling if we're not currently AND we're actually moving.
        if (component.IsCurrentlyWaddling || !args.HasDirectionalMovement)
            return;

        component.IsCurrentlyWaddling = true;

        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(uid)));
    }

    private void OnStood(EntityUid uid, WaddleAnimationComponent component, StoodEvent args)
    {
        // Prediction mitigation. Prediction means that MoveInputEvents are spammed repeatedly, even though you'd assume
        // they're once-only for the user actually doing something. As such do nothing if we're just repeating this FoR.
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (!TryComp<InputMoverComponent>(uid, out var mover))
        {
            return;
        }

        if ((mover.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            return;

        if (component.IsCurrentlyWaddling)
            return;

        component.IsCurrentlyWaddling = true;

        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(uid)));
    }

    private void StopWaddling(EntityUid uid, WaddleAnimationComponent component)
    {
        component.IsCurrentlyWaddling = false;

        RaiseNetworkEvent(new StoppedWaddlingEvent(GetNetEntity(uid)));
    }
}
