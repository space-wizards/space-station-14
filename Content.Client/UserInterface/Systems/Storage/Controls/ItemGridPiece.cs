using System.Linq;
using System.Numerics;
using Content.Client.Clickable;
using Content.Client.Guidebook.Richtext;
using Content.Client.Items.Systems;
using Content.Shared.Item;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed class ItemGridPiece : Control
{
    //todo maybe don't do DI
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IClickMapManager _clickMapManager = default!;
    private IEntityManager _entityManager;
    private ItemSystem _itemSystem;
    private SpriteSystem _spriteSystem;

    private EntityUid _uid;

    public bool Hovered;
    private List<(Texture, Vector2)> _textures = new();

    #region Textures
    //todo reloading
    private Texture? _centerTexture;
    private Texture? _topTexture;
    private Texture? _bottomTexture;
    private Texture? _leftTexture;
    private Texture? _rightTexture;
    private Texture? _topLeftTexture;
    private Texture? _topRightTexture;
    private Texture? _bottomLeftTexture;
    private Texture? _bottomRightTexture;

    private readonly string _centerTexturePath = "Storage/piece_center";
    private readonly string _topTexturePath = "Storage/piece_top";
    private readonly string _bottomTexturePath = "Storage/piece_bottom";
    private readonly string _leftTexturePath = "Storage/piece_left";
    private readonly string _rightTexturePath = "Storage/piece_right";
    private readonly string _topLeftTexturePath = "Storage/piece_topLeft";
    private readonly string _topRightTexturePath = "Storage/piece_topRight";
    private readonly string _bottomLeftTexturePath = "Storage/piece_bottomLeft";
    private readonly string _bottomRightTexturePath = "Storage/piece_bottomRight";
    #endregion

    public ItemGridPiece(Entity<ItemComponent> entity, IEntityManager entityManager)
    {
        IoCManager.InjectDependencies(this);

        _entityManager = entityManager;
        _itemSystem = entityManager.System<ItemSystem>();
        _spriteSystem = entityManager.System<SpriteSystem>();

        _uid = entity.Owner;

        _centerTexture = Theme.ResolveTextureOrNull(_centerTexturePath)?.Texture;
        _topTexture = Theme.ResolveTextureOrNull(_topTexturePath)?.Texture;
        _bottomTexture = Theme.ResolveTextureOrNull(_bottomTexturePath)?.Texture;
        _leftTexture = Theme.ResolveTextureOrNull(_leftTexturePath)?.Texture;
        _rightTexture = Theme.ResolveTextureOrNull(_rightTexturePath)?.Texture;
        _topLeftTexture = Theme.ResolveTextureOrNull(_topLeftTexturePath)?.Texture;
        _topRightTexture = Theme.ResolveTextureOrNull(_topRightTexturePath)?.Texture;
        _bottomLeftTexture = Theme.ResolveTextureOrNull(_bottomLeftTexturePath)?.Texture;
        _bottomRightTexture = Theme.ResolveTextureOrNull(_bottomRightTexturePath)?.Texture;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var adjustedShape = _itemSystem.GetItemShape((_uid, null));
        var boundingGrid = SharedStorageSystem.GetBoundingBox(adjustedShape);
        var size = _centerTexture!.Size * 2 * UIScale;

        _textures.Clear();
        for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
        {
            for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
            {
                if (!adjustedShape.Any(p => p.Contains(x, y)))
                    continue;

                var offset = size * new Vector2(x - boundingGrid.Left, y - boundingGrid.Bottom);
                var topLeft = PixelPosition + offset.Floored();

                if (GetTexture(adjustedShape, new Vector2i(x, y)) is not {} texture)
                    continue;

                _textures.Add((texture, GlobalPixelPosition + offset));
                handle.DrawTextureRect(texture, new UIBox2(topLeft, topLeft + size));
            }
        }

        var iconOffset = new Vector2(((boundingGrid.Width + 1) / 2f) * size.X ,
            ((boundingGrid.Height + 1) / 2f) * size.Y);

        handle.DrawEntity(_uid,
            PixelPosition + iconOffset,
            Vector2.One * 2 * UIScale,
            Angle.Zero,
            overrideDirection: Direction.South);
    }

    //todo you braindead idiot none of this shit works at all
    //cut all the textures in half and then do this shit again, possibly with an enum.
    private Texture? GetTexture(IReadOnlyList<Box2i> boxes, Vector2i position)
    {
        var top = !boxes.Any(b => b.Contains(position - Vector2i.Up));
        var bottom = !boxes.Any(b => b.Contains(position - Vector2i.Down));
        var left = !boxes.Any(b => b.Contains(position + Vector2i.Left));
        var right = !boxes.Any(b => b.Contains(position + Vector2i.Right));

        if (top && left)
            return _topLeftTexture;
        if (top && right)
            return _topRightTexture;
        if (bottom && left)
            return _bottomLeftTexture;
        if (bottom && right)
            return _bottomRightTexture;

        if (top)
            return _topTexture;
        if (bottom)
            return _bottomTexture;
        if (left)
            return _leftTexture;
        if (right)
            return _rightTexture;

        return _centerTexture;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_textures.Count == 0)
            return;

        if (!_inputManager.MouseScreenPosition.IsValid)
            return;

        var pos = _inputManager.MouseScreenPosition.Position;

        foreach (var (texture, position) in _textures)
        {
            var origin = position;
            if (!new Box2(origin, origin + (texture.Size * UIScale * 2)).Contains(pos))
                continue;

            Hovered = true;
            return;
        }

        Hovered = false;
    }
}
