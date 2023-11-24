using System.Linq;
using System.Numerics;
using Content.Client.Items.Systems;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Serilog;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed class ItemGridPiece : Control
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    private readonly ItemSystem _itemSystem;

    private readonly List<(Texture, Vector2)> _texturesPositions = new();
    private readonly List<(Texture, Vector2)> _texturesPixelPositions = new();

    public readonly EntityUid Entity;
    public readonly ItemStorageLocation Location;
    public bool Hovered;

    public event Action<GUIBoundKeyEventArgs, ItemGridPiece>? OnPiecePressed;
    public event Action<GUIBoundKeyEventArgs, ItemGridPiece>? OnPieceUnpressed;

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

    public ItemGridPiece(Entity<ItemComponent> entity, ItemStorageLocation location,  IEntityManager entityManager)
    {
        IoCManager.InjectDependencies(this);

        _itemSystem = entityManager.System<ItemSystem>();

        Entity = entity.Owner;
        Location = location;

        Visible = true;
        MouseFilter = MouseFilterMode.Pass;

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

        var adjustedShape = _itemSystem.GetAdjustedItemShape((Entity, null), Location.Rotation, Vector2i.Zero);
        var boundingGrid = SharedStorageSystem.GetBoundingBox(adjustedShape);
        var size = _centerTexture!.Size * 2 * UIScale;

        //todo recolor the sprites so that this works
        Color? colorModulate = Hovered ? Color.Red: null;

        _texturesPixelPositions.Clear();
        for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
        {
            for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
            {
                if (!adjustedShape.Any(p => p.Contains(x, y)))
                    continue;

                var offset = size * 2 * new Vector2(x - boundingGrid.Left, y - boundingGrid.Bottom);
                var topLeft = PixelPosition + offset.Floored();

                if (GetTexture(adjustedShape, new Vector2i(x, y), Direction.NorthEast) is {} neTexture)
                {
                    var neOffset = new Vector2(size.X, 0);
                    handle.DrawTextureRect(neTexture, new UIBox2(topLeft + neOffset, topLeft + neOffset + size), colorModulate);
                }
                if (GetTexture(adjustedShape, new Vector2i(x, y), Direction.NorthWest) is {} nwTexture)
                {
                    _texturesPositions.Add((nwTexture, Position + (offset / UIScale)));
                    _texturesPixelPositions.Add((nwTexture, GlobalPixelPosition + offset));
                    handle.DrawTextureRect(nwTexture, new UIBox2(topLeft, topLeft + size), colorModulate);
                }
                if (GetTexture(adjustedShape, new Vector2i(x, y), Direction.SouthEast) is {} seTexture)
                {
                    var seOffset = size;
                    handle.DrawTextureRect(seTexture, new UIBox2(topLeft + seOffset, topLeft + seOffset + size), colorModulate);
                }
                if (GetTexture(adjustedShape, new Vector2i(x, y), Direction.SouthWest) is {} swTexture)
                {
                    var swOffset = new Vector2(0, size.Y);
                    handle.DrawTextureRect(swTexture, new UIBox2(topLeft + swOffset, topLeft + swOffset + size), colorModulate);
                }
            }
        }

        // typically you'd divide by two, but since the textures are half a tile, this is done implicitly
        var iconOffset = new Vector2((boundingGrid.Width + 1) * size.X ,
            (boundingGrid.Height + 1) * size.Y);

        handle.DrawEntity(Entity,
            PixelPosition + iconOffset,
            Vector2.One * 2 * UIScale,
            Angle.Zero,
            overrideDirection: Direction.South);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_texturesPixelPositions.Count == 0)
            return;

        if (!_inputManager.MouseScreenPosition.IsValid)
            return;

        var pos = _inputManager.MouseScreenPosition.Position;

        foreach (var (texture, position) in _texturesPixelPositions)
        {
            var origin = position;
            if (!new Box2(origin, origin + texture.Size * UIScale * 4).Contains(pos))
                continue;

            Hovered = true;
            return;
        }

        Hovered = false;
    }

    protected override bool HasPoint(Vector2 point)
    {
        foreach (var (texture, position) in _texturesPositions)
        {
            if (!new Box2(position, position + texture.Size * 4).Contains(point))
                continue;

            return true;
        }

        return false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        OnPiecePressed?.Invoke(args, this);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        OnPieceUnpressed?.Invoke(args, this);
    }

    private Texture? GetTexture(IReadOnlyList<Box2i> boxes, Vector2i position, Direction corner)
    {
        var top = !boxes.Any(b => b.Contains(position - Vector2i.Up));
        var bottom = !boxes.Any(b => b.Contains(position - Vector2i.Down));
        var left = !boxes.Any(b => b.Contains(position + Vector2i.Left));
        var right = !boxes.Any(b => b.Contains(position + Vector2i.Right));

        switch (corner)
        {
            case Direction.NorthEast:
                if (top && right)
                    return _topRightTexture;
                if (top)
                    return _topTexture;
                if (right)
                    return _rightTexture;
                return _centerTexture;
            case Direction.NorthWest:
                if (top && left)
                    return _topLeftTexture;
                if (top)
                    return _topTexture;
                if (left)
                    return _leftTexture;
                return _centerTexture;
            case Direction.SouthEast:
                if (bottom && right)
                    return _bottomRightTexture;
                if (bottom)
                    return _bottomTexture;
                if (right)
                    return _rightTexture;
                return _centerTexture;
            case Direction.SouthWest:
                if (bottom && left)
                    return _bottomLeftTexture;
                if (bottom)
                    return _bottomTexture;
                if (left)
                    return _leftTexture;
                return _centerTexture;
            default:
                return null;
        }
    }
}
