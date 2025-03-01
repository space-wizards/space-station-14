using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
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
