using System.Numerics;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Examine;

public sealed partial class NameExamineOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly NameExamineSystem _nameExamineSystem;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _shader;

    private readonly Font _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public NameExamineOverlay(
        SpriteSystem sprite,
        SharedTransformSystem transform,
        NameExamineSystem nameExamine)
    {
        _sprite = sprite;
        _transform = transform;
        _nameExamineSystem = nameExamine;

        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index(UnshadedShader).Instance();

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/Grand9k/grand9k-pixel-unicode.otf"), 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null || !_nameExamineSystem.Held)
            return;

        if (_playerManager.LocalEntity is not { } playerEnt)
            return;

    args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.DrawingHandle.UseShader(_shader);
        var scale = _configManager.GetCVar(CVars.DisplayUIScale);

        if (scale == 0f)
            scale = _uiManager.DefaultUIScale;

        DrawWorld(args.ScreenHandle, args, playerEnt, scale);

        args.DrawingHandle.UseShader(null);
    }

    private void DrawWorld(DrawingHandleScreen handle, OverlayDrawArgs args, EntityUid playerEnt, float scale)
    {
        if (args.ViewportControl == null)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();

        var query = _entityManager.EntityQueryEnumerator<TransformComponent, SpriteComponent, MobStateComponent>();

        while (query.MoveNext(out var uid, out var xform, out var sprite, out _))
        {
            if (uid == playerEnt)
                continue;

            if (!_transform.InRange(playerEnt, uid, 10f))
               continue;

            var mapPos = _transform.ToMapCoordinates(xform.Coordinates);
            var pos = Vector2.Transform(mapPos.Position, matrix);

            var text = Identity.Name(uid, _entityManager, playerEnt);

            //Text dimensions
            var dimensions = handle.GetDimensions(_font, text, scale);

            //Get sprite bounding box so we can draw at the bottom.
            //Probably a better way to do this but I don't want it drawing over sprites if possible
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
            var bounds = _sprite.CalculateBounds((uid, sprite), worldPos, worldRot, args.Viewport.Eye?.Rotation ?? default);

            var drawPosition = (pos - dimensions / 2f) + new Vector2(0, bounds.Box.Extents.Y * matrix.M11);

            var outline = new TextOutline(2.5f, Color.Black);
            handle.DrawString(_font, drawPosition, text, scale, Color.LightGray.WithAlpha(200), outline);
        }
    }
}
