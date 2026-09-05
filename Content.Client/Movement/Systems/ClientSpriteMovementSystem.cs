using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Systems;

/// <inheritdoc/>
public sealed partial class ClientSpriteMovementSystem : SpriteMovementSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    [SubscribeLocalEvent]
    private void AfterHandleState(Entity<SpriteMovementComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    protected override void UpdateSprite(Entity<SpriteMovementComponent> ent)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var layers = ent.Comp.IsMoving ? ent.Comp.MovementLayers : ent.Comp.NoMovementLayers;
        foreach (var (layer, state) in layers)
        {
            _sprite.LayerSetData((ent, sprite), layer, state);
        }
    }
}
