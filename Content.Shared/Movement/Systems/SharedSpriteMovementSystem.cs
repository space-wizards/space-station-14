using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedSpriteMovementSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    [SubscribeLocalEvent]
    private void OnSpriteMoveInput(Entity<SpriteMovementComponent> ent, ref SpriteMoveEvent args)
    {
        var isMoving = args.IsMoving && _actionBlocker.CanMove(ent);
        if (ent.Comp.IsMoving == isMoving)
            return;

        ent.Comp.IsMoving = isMoving;
        Dirty(ent);
    }
}
