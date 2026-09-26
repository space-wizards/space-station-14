using System.Numerics;
using Content.Client.Light;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Profiling;
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
    [Dependency] private ProfManager _prof = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SharedMapSystem _mapSys;

    private readonly EntityQuery<OccluderComponent> _occluderQuery;
    private readonly EntityQuery<TransformComponent> _transformQuery;

    private static readonly ProtoId<ShaderPrototype> Shader = "XRayVision";
    private readonly ShaderInstance _tileShader;

    private const int TileSizePixels = EyeManager.PixelsPerMeter;
    public const int ContentZIndex = BeforeLightTargetOverlay.ContentZIndex + 1;

    private List<Entity<MapGridComponent>> _grids = [];

    public Color TileOverlayColor { get; private set; } = Color.White;
    public Color EntityOverlayColor { get; private set; } = Color.White;
    public float Scanlines { get; private set; } = 1f;
    public bool ShowTiles => true;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public XRayVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = ContentZIndex;
        _tileShader = _prototypeManager.Index(Shader).InstanceUnique();
        _transform = _entManager.System<SharedTransformSystem>();
        _mapSys = _entManager.System<SharedMapSystem>();
        _occluderQuery = _entManager.GetEntityQuery<OccluderComponent>();
        _transformQuery = _entManager.GetEntityQuery<TransformComponent>();
    }

    public void SetParameters(Color tileOverlayColor, Color entityOverlayColor, bool showTiles, float scanlines)
    {
        TileOverlayColor = tileOverlayColor;
        EntityOverlayColor = entityOverlayColor;
        Scanlines = scanlines;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewer = _player.LocalSession?.AttachedEntity;
        if (viewer == null)
            return;

        if (!_transformQuery.TryGetComponent(viewer.Value, out var viewerXform))
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

        if (ShowTiles)
        {
            handle.UseShader(_tileShader);
            DrawTiles(args, handle);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTiles(in OverlayDrawArgs args, DrawingHandleWorld handle)
    {
        using var _ = _prof.Group("XRayVisionOverlay.DrawTiles");
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
}
