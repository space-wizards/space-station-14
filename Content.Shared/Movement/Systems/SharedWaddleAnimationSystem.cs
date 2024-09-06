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
        SubscribeLocalEvent<WaddleAnimationComponent, StoodEvent>(OnStood);

        // Stop moving possibilities
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref StunnedEvent _) => StopWaddling(ent));
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref DownedEvent _) => StopWaddling(ent));
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref BuckledEvent _) => StopWaddling(ent));
        SubscribeLocalEvent<WaddleAnimationComponent, GravityChangedEvent>(OnGravityChanged);
    }

    private void OnGravityChanged(Entity<WaddleAnimationComponent> ent, ref GravityChangedEvent args)
    {
        if (!args.HasGravity && ent.Comp.IsCurrentlyWaddling)
            StopWaddling(ent);
    }

    private void OnComponentStartup(Entity<WaddleAnimationComponent> entity, ref ComponentStartup args)
    {
        if (!TryComp<InputMoverComponent>(entity.Owner, out var moverComponent))
            return;

        // If the waddler is currently moving, make them start waddling
        if ((moverComponent.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.AnyDirection)
        {
            RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(entity.Owner)));
        }
    }

    private void OnMovementInput(Entity<WaddleAnimationComponent> entity, ref MoveInputEvent args)
    {
        // Prediction mitigation. Prediction means that MoveInputEvents are spammed repeatedly, even though you'd assume
        // they're once-only for the user actually doing something. As such do nothing if we're just repeating this FoR.
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (!args.HasDirectionalMovement && entity.Comp.IsCurrentlyWaddling)
        {
            StopWaddling(entity);

            return;
        }

        // Only start waddling if we're not currently AND we're actually moving.
        if (entity.Comp.IsCurrentlyWaddling || !args.HasDirectionalMovement)
            return;

        entity.Comp.IsCurrentlyWaddling = true;

        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(entity.Owner)));
    }

    private void OnStood(Entity<WaddleAnimationComponent> entity, ref StoodEvent args)
    {
        // Prediction mitigation. Prediction means that MoveInputEvents are spammed repeatedly, even though you'd assume
        // they're once-only for the user actually doing something. As such do nothing if we're just repeating this FoR.
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (!TryComp<InputMoverComponent>(entity.Owner, out var mover))
        {
            return;
        }

        if ((mover.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            return;

        if (entity.Comp.IsCurrentlyWaddling)
            return;

        entity.Comp.IsCurrentlyWaddling = true;

        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(entity.Owner)));
    }

    private void StopWaddling(Entity<WaddleAnimationComponent> entity)
    {
        entity.Comp.IsCurrentlyWaddling = false;

        RaiseNetworkEvent(new StoppedWaddlingEvent(GetNetEntity(entity.Owner)));
    }
}
