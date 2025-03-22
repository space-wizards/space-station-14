using System.Numerics;
using Content.Shared.Decals;
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
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    private readonly DecalPrototype? _decal;
    private readonly Angle _rotation;
    private readonly Color _color;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CrayonDecalPlacementOverlay(SharedTransformSystem transform, SpriteSystem sprite, SharedInteractionSystem interaction, DecalPrototype? decal, Angle rotation, Color color)
    {
        IoCManager.InjectDependencies(this);
        _transform = transform;
        _sprite = sprite;
        _interaction = interaction;
        _decal = decal;
        _rotation = rotation;
        _color = color;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.PixelToMap(mouseScreenPos);

        var playerEnt = _playerManager.LocalSession?.AttachedEntity;
        if (playerEnt == null)
            return false;

        // only show preview decal if it is within range to be drawn
        return _interaction.InRangeUnobstructed(mousePos, playerEnt.Value, collisionMask: Shared.Physics.CollisionGroup.None);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_decal == null)
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
        var box = new Box2Rotated(aabb, _rotation, localPos);

        handle.DrawTextureRect(_sprite.Frame0(_decal.Sprite), box, _color);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
