using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Systems;

public sealed class MovementIgnoreGravitySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MovementAlwaysTouchingComponent, CanWeightlessMoveEvent>(OnWeightless);
        SubscribeLocalEvent<MovementIgnoreGravityComponent, IsWeightlessEvent>(OnIsWeightless);
    }

    private void OnIsWeightless(Entity<MovementIgnoreGravityComponent> ent, ref IsWeightlessEvent args)
    {
        args.Handled = true;
        args.IsWeightless = false;
    }

    private void OnWeightless(EntityUid uid, MovementAlwaysTouchingComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }
}
