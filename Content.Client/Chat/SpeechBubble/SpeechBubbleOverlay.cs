using System.Numerics;
using Content.Client.Examine;
using Content.Client.UserInterface.Systems.Chat;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Chat.SpeechBubble;

public sealed partial class SpeechBubbleOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly ChatUIController _chatUIController;

    private readonly EntityLookupSystem _lookup;
    private readonly ExamineSystem _examineSystem;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _shader;

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<TransformComponent> _transformQuery;


    public SpeechBubbleOverlay(
        EntityLookupSystem lookup,
        SpriteSystem sprite,
        SharedTransformSystem transform,
        ExamineSystem examine,
        EntityQuery<SpriteComponent> spriteQuery,
        EntityQuery<TransformComponent> transformQuery)
    {
        _lookup = lookup;
        _sprite = sprite;
        _transform = transform;
        _examineSystem = examine;

        _spriteQuery = spriteQuery;
        _transformQuery = transformQuery;

        IoCManager.InjectDependencies(this);

        _chatUIController = _uiManager.GetUIController<ChatUIController>();

        _shader = _prototypeManager.Index(UnshadedShader).Instance();

        var cache = IoCManager.Resolve<IResourceCache>();
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
