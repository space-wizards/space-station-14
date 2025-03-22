using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
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
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly HashSet<FadingSpriteComponent> _comps = new();

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<SpriteFadeComponent> _fadeQuery;
    private EntityQuery<FadingSpriteComponent> _fadingQuery;

    private (MapCoordinates Point, bool ExcludeBoundingBox)[] CoordinatesBuffer = new (MapCoordinates, bool)[2]; // we don't have stackalloc in client so best we can do is this

    private const float TargetAlpha = 0.4f;
    private const float ChangeRate = 1f;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _fadeQuery = GetEntityQuery<SpriteFadeComponent>();
        _fadingQuery = GetEntityQuery<FadingSpriteComponent>();

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
        var change = ChangeRate * frameTime;

        var length = 0;

        if (_uiManager.CurrentlyHovered is IViewportControl vp
            && _inputManager.MouseScreenPosition.IsValid)
        {
            CoordinatesBuffer[length++] = (vp.PixelToMap(_inputManager.MouseScreenPosition.Position), true);
        }

        if (TryComp(player, out TransformComponent? playerXform))
        {
            CoordinatesBuffer[length++] = (_transform.GetMapCoordinates(_playerManager.LocalEntity!.Value, xform: playerXform), false);
        }

        if (_stateManager.CurrentState is GameplayState state && _spriteQuery.TryGetComponent(player, out var playerSprite))
        {
            for (int i = 0; i < length; i++)
            {
                var (mapPos, excludeBB) = CoordinatesBuffer[i];

                // Also want to handle large entities even if they may not be clickable.
                foreach (var ent in state.GetClickableEntities(mapPos, excludeFaded: false))
                {
                    if (ent == player ||
                        !_fadeQuery.HasComponent(ent) ||
                        !_spriteQuery.TryGetComponent(ent, out var sprite) ||
                        sprite.DrawDepth < playerSprite.DrawDepth)
                    {
                        continue;
                    }

                    if (excludeBB)
                    {
                        var delta = new Vector2(10f * float.Epsilon, 10f * float.Epsilon);
                        var test = new Box2Rotated(mapPos.Position - delta, mapPos.Position + delta);
                        bool collided = false;
                        foreach (var fixture in _physics.GetCollidingEntities(mapPos.MapId, test))
                        {
                            if (fixture.Owner == ent)
                            {
                                collided = true;
                                break;
                            }
                        }
                        if (collided)
                        {
                            break;
                        }
                    }

                    if (!_fadingQuery.TryComp(ent, out var fading))
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
        }


        var query = AllEntityQuery<FadingSpriteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_comps.Contains(comp))
                continue;

            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
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
