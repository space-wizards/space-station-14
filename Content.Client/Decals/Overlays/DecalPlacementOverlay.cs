using System.Numerics;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Decals.Overlays;

[Virtual]
public class DecalPlacementOverlay : Overlay
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly DecalPlacementSystem _placement;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    protected DecalPrototype? decal;
    protected bool snap;
    protected Angle rotation;
    protected Color? color;

    public DecalPlacementOverlay(DecalPlacementSystem placement, SharedTransformSystem transform, SpriteSystem sprite)
    {
        IoCManager.InjectDependencies(this);
        _placement = placement;
        _transform = transform;
        _sprite = sprite;
        ZIndex = 1000;
    }

    protected virtual void LoadDecal()
    {
        (decal, snap, rotation, color) = _placement.GetActiveDecal();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        LoadDecal();

        if (decal == null)
            return;

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.PixelToMap(mouseScreenPos);

        if (mousePos.MapId != args.MapId)
            return;

        // No map support for decals
        if (!_mapManager.TryFindGridAt(mousePos, out var gridUid, out var grid))
        {
            return;
        }

        var worldMatrix = _transform.GetWorldMatrix(gridUid);
        var invMatrix = _transform.GetInvWorldMatrix(gridUid);

        var handle = args.WorldHandle;
        handle.SetTransform(worldMatrix);

        var localPos = Vector2.Transform(mousePos.Position, invMatrix);

        if (snap)
        {
            localPos = localPos.Floored() + grid.TileSizeHalfVector;
        }

        // Nothing uses snap cardinals so probably don't need preview?
        var aabb = Box2.UnitCentered.Translated(localPos);
        var box = new Box2Rotated(aabb, rotation, localPos);

        handle.DrawTextureRect(_sprite.Frame0(decal.Sprite), box, color);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
