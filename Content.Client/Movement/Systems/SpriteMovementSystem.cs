using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Systems;

/// <summary>
/// Controls the switching of motion and standing still animation
/// </summary>
public sealed partial class SpriteMovementSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<ActiveInputMoverComponent> _activeInputMover = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    [SubscribeLocalEvent]
    private void OnSpriteMoveInput(Entity<SpriteMovementComponent> ent, ref MoveInputEvent args)
    {
        var isMoving = args.HasDirectionalMovement && _activeInputMover.HasComp(ent);
        if (ent.Comp.IsMoving == isMoving)
            return;

        ent.Comp.IsMoving = isMoving;

        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var layers = isMoving ? ent.Comp.MovementLayers : ent.Comp.NoMovementLayers;
        foreach (var (layer, state) in layers)
        {
            _sprite.LayerSetData((ent, sprite), layer, state);
        }
    }
}
