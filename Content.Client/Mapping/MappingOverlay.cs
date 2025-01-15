using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.Client.Mapping.MappingState;

namespace Content.Client.Mapping;

public sealed class MappingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

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
            if (_entities.TryGetComponent(id, out SpriteComponent? sprite))
                sprite.Color = color;
        }

        _oldColors.Clear();

        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        switch (_state.Meta.State)
        {
            case CursorState.Tile:
            {
                if (_state.GetHoveredTileBox2() is { } box)
                    args.WorldHandle.DrawRect(box, _state.Meta.Color);

                break;
            }
            case CursorState.Decal:
            {
                if (_state.GetHoveredDecalData() is { } hovered)
                {
                    var (texture, box) = hovered;
                    args.WorldHandle.DrawTextureRect(texture, box, _state.Meta.Color);
                }

                break;
            }
            case CursorState.Entity:
            {
                if (_state.GetHoveredEntity() is { } entity &&
                    _entities.TryGetComponent(entity, out SpriteComponent? sprite))
                {
                    _oldColors[entity] = sprite.Color;
                    sprite.Color = _state.Meta.Color;
                }

                break;
            }
            case CursorState.Grid:
            {
                if (args.MapId == MapId.Nullspace || _state.GetHoveredGrid() is not { } grid)
                    break;

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
                    args.WorldHandle.DrawRect(box, _state.Meta.Color);
                }

                break;
            }
            case CursorState.EntityOrTile:
            {
                if (_state.GetHoveredEntity() is { } entity &&
                    _entities.TryGetComponent(entity, out SpriteComponent? sprite))
                {
                    _oldColors[entity] = sprite.Color;
                    sprite.Color = _state.Meta.Color;
                }
                else if (_state.GetHoveredTileBox2() is { } box)
                {
                    args.WorldHandle.DrawRect(box, _state.Meta.SecondColor ?? _state.Meta.Color);
                }

                break;
            }
        }

        handle.UseShader(null);
    }
}
