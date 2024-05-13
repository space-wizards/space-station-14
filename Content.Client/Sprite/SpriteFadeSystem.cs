using Content.Client.Gameplay;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Physics;

namespace Content.Client.Sprite;

public sealed class SpriteFadeSystem : EntitySystem
{
    /*
     * If the player entity is obstructed under the specified components then it will drop the alpha for that entity
     * so the player is still visible.
     */

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<FadingSpriteComponent> _comps = new();

    private const float TargetAlpha = 0.4f;
    private const float ChangeRate = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FadingSpriteComponent, ComponentShutdown>(OnFadingShutdown);
    }

    private void OnFadingShutdown(EntityUid uid, FadingSpriteComponent component, ComponentShutdown args)
    {
        if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Color = sprite.Color.WithAlpha(component.OriginalAlpha);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _playerManager.LocalEntity;
        var spriteQuery = GetEntityQuery<SpriteComponent>();
        var change = ChangeRate * frameTime;

        if (TryComp<TransformComponent>(player, out var playerXform) &&
            _stateManager.CurrentState is GameplayState state &&
            spriteQuery.TryGetComponent(player, out var playerSprite))
        {
            var fadeQuery = GetEntityQuery<SpriteFadeComponent>();
            var mapPos = _transform.GetMapCoordinates(_playerManager.LocalEntity!.Value, xform: playerXform);

            // Also want to handle large entities even if they may not be clickable.
            foreach (var ent in state.GetClickableEntities(mapPos))
            {
                if (ent == player ||
                    !fadeQuery.HasComponent(ent) ||
                    !spriteQuery.TryGetComponent(ent, out var sprite) ||
                    sprite.DrawDepth < playerSprite.DrawDepth)
                {
                    continue;
                }

                if (!TryComp<FadingSpriteComponent>(ent, out var fading))
                {
                    fading = AddComp<FadingSpriteComponent>(ent);
                    fading.OriginalAlpha = sprite.Color.A;
                }

                _comps.Add(fading);
                var newColor = Math.Max(sprite.Color.A - change, TargetAlpha);

                if (!sprite.Color.A.Equals(newColor))
                {
                    sprite.Color = sprite.Color.WithAlpha(newColor);
                }
            }
        }

        var query = AllEntityQuery<FadingSpriteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_comps.Contains(comp))
                continue;

            if (!spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            var newColor = Math.Min(sprite.Color.A + change, comp.OriginalAlpha);

            if (!newColor.Equals(sprite.Color.A))
            {
                sprite.Color = sprite.Color.WithAlpha(newColor);
            }
            else
            {
                RemCompDeferred<FadingSpriteComponent>(uid);
            }
        }

        _comps.Clear();
    }
}
