using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

/// <summary>
/// Controls the switching of motion and standing still animation
/// </summary>
public sealed class ClientSpriteMovementSystem : SharedSpriteMovementSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<SpriteMovementComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<SpriteMovementComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_spriteQuery.TryGetComponent(ent, out var sprite))
            return;

        if (ent.Comp.IsMoving)
        {
            foreach (var (layer, state) in ent.Comp.MovementLayers)
            {
                sprite.LayerSetData(layer, state);
            }
        }
        else
        {
            foreach (var (layer, state) in ent.Comp.NoMovementLayers)
            {
                sprite.LayerSetData(layer, state);
            }
        }
    }
}
