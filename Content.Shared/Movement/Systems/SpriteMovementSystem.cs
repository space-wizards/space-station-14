using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

// TODO: Add public API for changing SpriteMovementComponent since SharedBorgSwitchableTypeSystem changes them directly
/// <summary>
/// Controls the switching of motion and standing still animation
/// </summary>
public abstract partial class SpriteMovementSystem : EntitySystem
{
    [Dependency] private EntityQuery<ActiveInputMoverComponent> _activeInputMover = default!;

    [SubscribeLocalEvent]
    private void OnSpriteMoveInput(Entity<SpriteMovementComponent> ent, ref MoveInputEvent args)
    {
        var isMoving = args.HasDirectionalMovement && _activeInputMover.HasComp(ent);
        if (ent.Comp.IsMoving == isMoving)
            return;

        ent.Comp.IsMoving = isMoving;
        Dirty(ent);

        UpdateSprite(ent);
    }

    protected virtual void UpdateSprite(Entity<SpriteMovementComponent> ent) { }
}
