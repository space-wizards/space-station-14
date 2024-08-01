using System.Numerics;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Crayon.Overlays;

public sealed class CrayonDecalPlacementOverlay : Overlay
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private readonly SharedInteractionSystem _interaction;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly CrayonSystem _crayon;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CrayonDecalPlacementOverlay(CrayonSystem crayon, SharedTransformSystem transform, SpriteSystem sprite, SharedInteractionSystem interaction)
    {
        IoCManager.InjectDependencies(this);
        _transform = transform;
        _sprite = sprite;
        _crayon = crayon;
        _interaction = interaction;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.PixelToMap(mouseScreenPos);

        var playerEnt = _playerManager.LocalSession?.AttachedEntity;
        if (playerEnt == null)
            return false;

        var playerPos = _transform.GetMapCoordinates(playerEnt.Value);

        return _interaction.InRangeUnobstructed(playerPos, mousePos);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var (decal, rotation, color) = _crayon.GetActiveDecal();

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

        // Nothing uses snap cardinals so probably don't need preview?
        var aabb = Box2.UnitCentered.Translated(localPos);
        var box = new Box2Rotated(aabb, rotation, localPos);

        handle.DrawTextureRect(_sprite.Frame0(decal.Sprite), box, color);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
