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
using Robust.Shared.Physics.Components;

namespace Content.Client.Sprite;

public sealed class SpriteFadeSystem : EntitySystem
{
    /*
     * If the player entity is obstructed under the specified components then it will drop the alpha for that entity
     * so the player is still visible.
     */

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private List<(MapCoordinates Point, bool ExcludeBoundingBox)> _points = new();

    private readonly HashSet<FadingSpriteComponent> _comps = new();

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<SpriteFadeComponent> _fadeQuery;
    private EntityQuery<FadingSpriteComponent> _fadingQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    private const float TargetAlpha = 0.4f;
    private const float ChangeRate = 1f;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _fadeQuery = GetEntityQuery<SpriteFadeComponent>();
        _fadingQuery = GetEntityQuery<FadingSpriteComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        SubscribeLocalEvent<FadingSpriteComponent, ComponentShutdown>(OnFadingShutdown);
    }

    private void OnFadingShutdown(EntityUid uid, FadingSpriteComponent component, ComponentShutdown args)
    {
        if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Color = sprite.Color.WithAlpha(component.OriginalAlpha);
    }

    /// <summary>
    ///     Adds sprites to the fade set, and brings their alpha downwards
    /// </summary>
    private void FadeIn(float change)
    {
        var player = _playerManager.LocalEntity;
        // ExcludeBoundingBox is set if we don't want to fade this sprite within the collision bounding boxes for the given POI
        _points.Clear();

        if (_uiManager.CurrentlyHovered is IViewportControl vp
            && _inputManager.MouseScreenPosition.IsValid)
        {
            _points.Add((vp.PixelToMap(_inputManager.MouseScreenPosition.Position), true));
        }

        if (TryComp(player, out TransformComponent? playerXform))
        {
            _points.Add((_transform.GetMapCoordinates(_playerManager.LocalEntity!.Value, xform: playerXform), false));
        }

        if (_stateManager.CurrentState is GameplayState state && _spriteQuery.TryGetComponent(player, out var playerSprite))
        {
            foreach (var (mapPos, excludeBB) in _points)
            {
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

                    // If it intersects a fixture ignore it.
                    if (excludeBB && _fixturesQuery.TryComp(ent, out var body))
                    {
                        var transform = _physics.GetPhysicsTransform(ent);
                        var collided = false;

                        foreach (var fixture in body.Fixtures.Values)
                        {
                            if (!fixture.Hard)
                                continue;

                            if (_fixtures.TestPoint(fixture.Shape, transform, mapPos.Position))
                            {
                                collided = true;
                                break;
                            }
                        }

                        // Check next entity
                        if (collided)
                        {
                            continue;
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
    }

    /// <summary>
    ///     Bring sprites back up to their original alpha if they aren't in the fade set, and removes their fade component when done
    /// </summary>
    private void FadeOut(float change)
    {
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
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var change = ChangeRate * frameTime;

        FadeIn(change);
        FadeOut(change);

        _comps.Clear();
    }
}
