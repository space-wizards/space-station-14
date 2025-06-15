using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Systems;

public sealed class MovementIgnoreGravitySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MovementAlwaysTouchingComponent, CanWeightlessMoveEvent>(OnWeightless);
    }

    private void OnWeightless(EntityUid uid, MovementAlwaysTouchingComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }
}
