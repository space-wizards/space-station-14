using System.Numerics;
using Content.Client.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Light;
using Robust.Shared.Prototypes;

namespace Content.Client.NamePeek;

/// <summary>
/// Handles the name peek overlay.
/// Overlay will show names underneath mob entities when Visible is true in NamePeekSystem
/// </summary>
public sealed partial class NamePeekOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly LightLevelSystem _lightLevel;
    private readonly NamePeekSystem _namePeekSystem;
    private readonly ExamineSystem _examineSystem;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _shader;

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;

    private TextOutline _outline = new (2.5f, Color.Black);

    private readonly HashSet<Entity<MobStateComponent>> _nearbyEntities = new();

    private readonly Font _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public NamePeekOverlay(
        EntityLookupSystem lookup,
        SpriteSystem sprite,
        SharedTransformSystem transform,
        LightLevelSystem lightLevel,
        NamePeekSystem namePeek,
        ExamineSystem examine,
        EntityQuery<SpriteComponent> spriteQuery,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<MobStateComponent> mobStateQuery)
    {
        _lookup = lookup;
        _sprite = sprite;
        _transform = transform;
        _lightLevel = lightLevel;
        _namePeekSystem = namePeek;
        _examineSystem = examine;

        _spriteQuery = spriteQuery;
        _transformQuery = transformQuery;
        _mobStateQuery = mobStateQuery;


        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index(UnshadedShader).Instance();

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/Grand9k/grand9k-pixel-unicode.otf"), 21);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null || !_namePeekSystem.Visible)
            return false;

        //Don't draw names if we're crit
        if (_mobStateQuery.TryComp(_playerManager.LocalEntity, out var mobState)
            && (mobState.CurrentState == MobState.Critical))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        if (_playerManager.LocalEntity is not { } playerEnt)
            return;

        if (args.Viewport.Eye is not { } eye)
            return;

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.DrawingHandle.UseShader(_shader);
        var scale = _configManager.GetCVar(CVars.DisplayUIScale);

        if (scale == 0f)
            scale = _uiManager.DefaultUIScale;

        var handle = args.ScreenHandle;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();

        _nearbyEntities.Clear();
        _lookup.GetEntitiesIntersecting(args.MapId, args.WorldAABB, _nearbyEntities, LookupFlags.Uncontained);

        foreach (var ent in _nearbyEntities)
        {
            if (ent.Owner == playerEnt)
                continue;

            if (!_transformQuery.TryComp(ent, out var xform))
                continue;

            var mapPos = _transform.GetMapCoordinates((ent, xform));

            var lightLevel = 1f;
            if (eye.DrawLight)
                _lightLevel.TryCalculateLightLevel(mapPos, out lightLevel);

            if (lightLevel < 0.35)
                continue;

            if (!_spriteQuery.TryComp(ent, out var sprite))
                continue;

            if (eye.DrawFov && !_examineSystem.InRangeUnOccluded(playerEnt, ent))
                continue;

            var pos = Vector2.Transform(mapPos.Position, matrix);

            var text = Identity.Name(ent, _entityManager, playerEnt);

            //Text dimensions for centering
            var dimensions = handle.GetDimensions(_font, text, scale);

            //Get sprite bounding box so we can draw at the bottom.
            //Probably a better way to do this but I want it drawing at the bottom of entity sprites if possible.
            //Seems to work with every mob I've tried.
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
            var bounds = _sprite.CalculateBounds((ent, sprite),
                worldPos,
                worldRot,
                args.Viewport.Eye?.Rotation ?? default);

            var drawPosition = (pos - dimensions / 2f) + new Vector2(0, bounds.Box.Extents.Y * matrix.M11);

            handle.DrawString(_font, drawPosition, text, scale, Color.LightGray.WithAlpha(200), _outline);
        }

        args.DrawingHandle.UseShader(null);
    }
}
