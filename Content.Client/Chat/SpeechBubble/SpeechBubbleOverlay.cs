using System.Numerics;
using Content.Client.UserInterface.Systems.Chat;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Chat.SpeechBubble;

public sealed partial class SpeechBubbleOverlay : Overlay
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;

    private readonly ChatUIController _chatUIController;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<TransformComponent> _transformQuery;


    public SpeechBubbleOverlay(
        SpriteSystem sprite,
        SharedTransformSystem transform,
        EntityQuery<SpriteComponent> spriteQuery,
        EntityQuery<TransformComponent> transformQuery)
    {
        _sprite = sprite;
        _transform = transform;

        _spriteQuery = spriteQuery;
        _transformQuery = transformQuery;

        IoCManager.InjectDependencies(this);

        _chatUIController = _uiManager.GetUIController<ChatUIController>();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var (ent, controls) in _chatUIController.NuActiveSpeechBubbles)
        {
            foreach (var control in controls)
            {
                control.Update(args);
            }
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        if (_playerManager.LocalEntity is not { } playerEnt)
            return;

        if (args.Viewport.Eye is not { } eye)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();

        foreach (var (ent, controls) in _chatUIController.NuActiveSpeechBubbles)
        {
            if (!_transformQuery.TryComp(ent, out var xform))
                continue;

            if (!_spriteQuery.TryComp(ent, out var sprite))
                continue;

            //Get sprite bounding box so we can draw at the bottom.
            //Probably a better way to do this, but I want it drawing at the bottom of entity sprites if possible.
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
            var bounds = _sprite.CalculateBounds((ent, sprite),
                worldPos,
                worldRot,
                eye.Rotation);

            foreach (var control in controls)
            {
                var offset = (-eye.Rotation).ToWorldVec() * (bounds.Box.Extents.Y);
                var offsetWorldPos = worldPos - offset;

                var pos = Vector2.Transform(offsetWorldPos, matrix);
                var drawPosition = (pos - control.DesiredSize / 2f) - new Vector2(0, control.VerticalOffsetAchieved + control.ContentSize.Y/2);

                _uiManager.RenderControl(args.RenderHandle, control, drawPosition.Floored());
            }
        }
    }
}
