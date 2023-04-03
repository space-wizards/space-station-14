using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Decals.Overlays;

public sealed class DecalPlacementOverlay : Overlay
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly DecalPlacementSystem _placement;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public DecalPlacementOverlay(DecalPlacementSystem placement, SharedTransformSystem transform, SpriteSystem sprite)
    {
        IoCManager.InjectDependencies(this);
        _placement = placement;
        _transform = transform;
        _sprite = sprite;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var (decal, snap, rotation, color) = _placement.GetActiveDecal();

        if (decal == null)
            return;

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.ScreenToMap(mouseScreenPos);

        if (mousePos.MapId != args.MapId)
            return;

        // No map support for decals
        if (!_mapManager.TryFindGridAt(mousePos, out var grid))
        {
            return;
        }

        var worldMatrix = _transform.GetWorldMatrix(grid.Owner);
        var invMatrix = _transform.GetInvWorldMatrix(grid.Owner);

        var handle = args.WorldHandle;
        handle.SetTransform(worldMatrix);

        var localPos = invMatrix.Transform(mousePos.Position);

        if (snap)
        {
            localPos = (Vector2) localPos.Floored() + grid.TileSize / 2f;
        }

        // Nothing uses snap cardinals so probably don't need preview?
        var aabb = Box2.UnitCentered.Translated(localPos);
        var box = new Box2Rotated(aabb, rotation, localPos);

        handle.DrawTextureRect(_sprite.Frame0(decal.Sprite), box, color);
        handle.SetTransform(Matrix3.Identity);
    }
}
