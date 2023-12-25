using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Systems;

/// <summary>
/// Handles setting sprite states based on whether an entity has movement input.
/// </summary>
public sealed class SpriteMovementSystem : EntitySystem
{
    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpriteMovementComponent, MoveInputEvent>(OnSpriteMoveInput);
        _spriteQuery = GetEntityQuery<SpriteComponent>();
    }

    private void OnSpriteMoveInput(EntityUid uid, SpriteMovementComponent component, ref MoveInputEvent args)
    {
        var oldMoving = SharedMoverController.GetNormalizedMovement(args.OldMovement) != MoveButtons.None;
        var moving = SharedMoverController.GetNormalizedMovement(args.Component.HeldMoveButtons) != MoveButtons.None;

        if (oldMoving == moving || !_spriteQuery.TryGetComponent(uid, out var sprite))
            return;

        if (moving)
        {
            foreach (var (layer, state) in component.MovementLayers)
            {
                sprite.LayerSetState(layer, state);
            }
        }
        else
        {
            foreach (var (layer, state) in component.NoMovementLayers)
            {
                sprite.LayerSetState(layer, state);
            }
        }
    }
}
