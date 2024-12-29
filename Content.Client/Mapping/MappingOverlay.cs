using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.Client.Mapping.MappingState;

namespace Content.Client.Mapping;

public sealed class MappingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    // 1 off in case something else uses these colors since we use them to compare
    private static readonly Color PickColor = new(1, 255, 0);
    private static readonly Color DeleteColor = new(255, 1, 0);

    private readonly Dictionary<EntityUid, Color> _oldColors = new();

    private readonly MappingState _state;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MappingOverlay(MappingState state)
    {
        IoCManager.InjectDependencies(this);

        _state = state;
        _shader = _prototypes.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (id, color) in _oldColors)
        {
            if (!_entities.TryGetComponent(id, out SpriteComponent? sprite))
                continue;

            if (sprite.Color == DeleteColor || sprite.Color == PickColor)
                sprite.Color = color;
        }

        _oldColors.Clear();

        if (_player.LocalEntity == null)
            return;

        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        switch (_state.State)
        {
            case CursorState.Pick:
            {
                if (_state.GetHoveredEntity() is { } entity &&
                    _entities.TryGetComponent(entity, out SpriteComponent? sprite))
                {
                    _oldColors[entity] = sprite.Color;
                    sprite.Color = PickColor;
                }

                break;
            }
            case CursorState.Delete:
            {
                if (_state.GetHoveredEntity() is { } entity &&
                    _entities.TryGetComponent(entity, out SpriteComponent? sprite))
                {
                    _oldColors[entity] = sprite.Color;
                    sprite.Color = DeleteColor;
                }

                break;
            }
            case CursorState.GridGreen:
            {
                DrawGridOverlay(args, new Color(0f, 225f, 0f).WithAlpha(0.05f));
                break;
            }
            case CursorState.GridRed:
            {
                DrawGridOverlay(args, new Color(225f, 0f, 0f).WithAlpha(0.05f));
                break;
            }
        }

        handle.UseShader(null);
    }

    public void DrawGridOverlay(in OverlayDrawArgs args, Color color)
    {
        if (args.MapId == MapId.Nullspace || _state.GetHoveredGrid() is not { } grid)
            return;

        var mapSystem = _entities.System<SharedMapSystem>();
        var xformSystem = _entities.System<SharedTransformSystem>();

        var tileSize = grid.Comp.TileSize;
        var tileDimensions = new Vector2(tileSize, tileSize);
        var (_, _, worldMatrix, invMatrix) = xformSystem.GetWorldPositionRotationMatrixWithInv(grid.Owner);
        args.WorldHandle.SetTransform(worldMatrix);
        var bounds = args.WorldBounds;
        bounds = new Box2Rotated(bounds.Box.Enlarged(1), bounds.Rotation, bounds.Origin);
        var localAABB = invMatrix.TransformBox(bounds);

        var enumerator = mapSystem.GetLocalTilesEnumerator(grid.Owner, grid, localAABB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var box = Box2.FromDimensions(tileRef.GridIndices, tileDimensions);
            args.WorldHandle.DrawRect(box, color);
        }
    }
}
