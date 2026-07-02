using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

public abstract class SharedSpriteMovementSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteMovementComponent, SpriteMoveEvent>(OnSpriteMoveInput);
    }

    private void OnSpriteMoveInput(Entity<SpriteMovementComponent> ent, ref SpriteMoveEvent args)
    {
        var isMoving = args.IsMoving && _actionBlocker.CanMove(ent);
        if (ent.Comp.IsMoving == isMoving)
            return;

        ent.Comp.IsMoving = isMoving;
        Dirty(ent);
    }
}
