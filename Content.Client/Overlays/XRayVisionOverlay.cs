using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Overlay that shows tiles and entities hidden behind walls.
/// </summary>
public sealed partial class XRayVisionOverlay : Overlay
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IResourceCache _resCache = default!;
    [Dependency] private ITileDefinitionManager _tileDefManager = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SharedMapSystem _mapSys;
    private readonly SpriteSystem _sprite;
    private readonly SpriteTreeSystem _spriteTree;
    private readonly EntityWhitelistSystem _whitelist;

    private readonly EntityQuery<OccluderComponent> _occluderQuery;

    private static readonly ProtoId<ShaderPrototype> Shader = "XRayVision";
    private readonly ShaderInstance _tileShader;
    private readonly ShaderInstance _entityShader;

    private const int TileSizePixels = EyeManager.PixelsPerMeter;

    private List<Entity<MapGridComponent>> _grids = [];
    private readonly List<Entity<SpriteComponent, TransformComponent>> _entities = [];

    public Color TileOverlayColor { get; private set; } = Color.White;
    public Color EntityOverlayColor { get; private set; } = Color.White;
    public bool ShowTiles { get; private set; } = true;
    public float Scanlines { get; private set; } = 1f;
    public EntityWhitelist? Whitelist { get; private set; }
    public EntityWhitelist? Blacklist { get; private set; }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public XRayVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _tileShader = _prototypeManager.Index(Shader).InstanceUnique();
        _entityShader = _prototypeManager.Index(Shader).InstanceUnique();
        _transform = _entManager.System<SharedTransformSystem>();
        _mapSys = _entManager.System<SharedMapSystem>();
        _sprite = _entManager.System<SpriteSystem>();
        _spriteTree = _entManager.System<SpriteTreeSystem>();
        _whitelist = _entManager.System<EntityWhitelistSystem>();
        _occluderQuery = _entManager.GetEntityQuery<OccluderComponent>();
    }

    public void SetParameters(Color tileOverlayColor, Color entityOverlayColor, bool showTiles, float scanlines, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        TileOverlayColor = tileOverlayColor;
        EntityOverlayColor = entityOverlayColor;
        ShowTiles = showTiles;
        Scanlines = scanlines;
        Whitelist = whitelist;
        Blacklist = blacklist;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewer = _player.LocalSession?.AttachedEntity;
        if (viewer == null)
            return;

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(viewer.Value, out var viewerXform))
            return;

        if (viewerXform.MapID != args.MapId)
            return;

        var eye = args.Viewport.Eye;
        if (eye == null)
            return;

        var handle = args.WorldHandle;

        // Feed both shaders the FoV shadow map and their fixed colors.
        _tileShader.SetParameter("FOV_TEXTURE", args.Viewport.FovRenderTarget.Texture);
        _tileShader.SetParameter("FOV_CENTER", eye.Position.Position);
        _tileShader.SetParameter("OVERLAY_COLOR", TileOverlayColor);
        _tileShader.SetParameter("SCANLINES", Scanlines);

        _entityShader.SetParameter("FOV_TEXTURE", args.Viewport.FovRenderTarget.Texture);
        _entityShader.SetParameter("FOV_CENTER", eye.Position.Position);
        _entityShader.SetParameter("OVERLAY_COLOR", EntityOverlayColor);
        _entityShader.SetParameter("SCANLINES", Scanlines);

        if (ShowTiles)
        {
            handle.UseShader(_tileShader);
            DrawTiles(args, handle);
        }

        handle.UseShader(_entityShader);
        DrawEntities(args, handle, xformQuery);

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTiles(in OverlayDrawArgs args, DrawingHandleWorld handle)
    {
        _grids.Clear();
        _mapSys.FindGridsIntersecting(args.MapId, args.WorldAABB, ref _grids);

        foreach (var grid in _grids)
        {
            var gridWorldMatrix = _transform.GetWorldMatrix(grid.Owner);
            handle.SetTransform(gridWorldMatrix);

            foreach (var tileRef in _mapSys.GetTilesIntersecting(grid.Owner, grid.Comp, args.WorldAABB))
            {
                if (tileRef.Tile.IsEmpty)
                    continue;

                if (!_tileDefManager.TryGetDefinition(tileRef.Tile.TypeId, out var tileDef) || tileDef.Sprite is not { } sprite)
                    continue;

                // Skip tiles that have a wall on them.
                if (TileHasOccluder(grid, tileRef.GridIndices))
                    continue;

                var texture = _resCache.GetResource<TextureResource>(sprite).Texture;

                // Tile spritesheets lay variants out horizontally, each TileSizePixels wide.
                var variant = tileRef.Tile.Variant % tileDef.Variants;
                var subRegion = UIBox2.FromDimensions(variant * TileSizePixels, 0, TileSizePixels, TileSizePixels);

                // Draw the tile in grid-local space (transform already set above).
                var tileSize = new Vector2(grid.Comp.TileSize);
                var tilePosition = new Vector2(tileRef.GridIndices.X, tileRef.GridIndices.Y) * grid.Comp.TileSize;
                var tileTransform = Matrix3x2.CreateTranslation(tilePosition);
                handle.SetTransform(tileTransform * gridWorldMatrix);
                handle.DrawTextureRectRegion(texture, new Box2(Vector2.Zero, tileSize), null, subRegion);
                handle.SetTransform(gridWorldMatrix);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private bool TileHasOccluder(Entity<MapGridComponent> grid, Vector2i indices)
    {
        var anchored = _mapSys.GetAnchoredEntitiesEnumerator(grid.Owner, grid.Comp, indices);
        while (anchored.MoveNext(out var ent))
        {
            if (_occluderQuery.TryGetComponent(ent, out var occluder) && occluder.Enabled)
                return true;
        }

        return false;
    }

    private void DrawEntities(in OverlayDrawArgs args, DrawingHandleWorld handle, EntityQuery<TransformComponent> xformQuery)
    {
        if (Whitelist == null)
            return;

        _entities.Clear();
        _spriteTree.QueryAabb(_entities, args.MapId, args.WorldAABB);

        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        foreach (var ent in _entities)
        {
            var uid = ent.Owner;
            var sprite = ent.Comp1;
            var xform = ent.Comp2;

            if (!sprite.Visible || sprite.ContainerOccluded || !_whitelist.CheckBoth(uid, Blacklist, Whitelist))
                continue;

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            _sprite.RenderSprite((uid, sprite), handle, eyeRotation, worldRot, worldPos, overrideShader: _entityShader);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
