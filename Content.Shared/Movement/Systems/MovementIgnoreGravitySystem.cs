using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

public sealed class MovementIgnoreGravitySystem : EntitySystem
{
    [Dependency] SharedGravitySystem _gravity = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MovementAlwaysTouchingComponent, CanWeightlessMoveEvent>(OnWeightless);
        SubscribeLocalEvent<MovementIgnoreGravityComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<MovementIgnoreGravityComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnWeightless(Entity<MovementAlwaysTouchingComponent> entity, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnIsWeightless(Entity<MovementIgnoreGravityComponent> entity, ref IsWeightlessEvent args)
    {
        // We don't check if the event has been handled as this component takes precedent over other things.

        args.IsWeightless = entity.Comp.Weightless;
        args.Handled = true;
    }

    private void OnComponentStartup(Entity<MovementIgnoreGravityComponent> entity, ref ComponentStartup args)
    {
        EnsureComp<GravityAffectedComponent>(entity);
        _gravity.RefreshWeightless(entity.Owner, entity.Comp.Weightless);
    }
}
