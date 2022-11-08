using Content.Client.Parallax;
using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Weather;

public sealed class WeatherOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.WorldSpaceBelowWorld;

    private IRenderTexture _blep;

    public WeatherOverlay(SpriteSystem sprite)
    {
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _sprite = sprite;
        IoCManager.InjectDependencies(this);

        var clyde = IoCManager.Resolve<IClyde>();
        _blep = clyde.CreateRenderTarget(clyde.ScreenSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather");
    }

    // TODO: WeatherComponent on the map.
    // TODO: Fade-in
    // TODO: Scrolling(?) like parallax
    // TODO: Need affected tiles and effects to apply.

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        if (!_entManager.TryGetComponent<WeatherComponent>(_mapManager.GetMapEntityId(args.MapId), out var weather) ||
            weather.Weather == null)
        {
            return false;
        }

        return base.BeforeDraw(in args);

    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entManager.TryGetComponent<WeatherComponent>(_mapManager.GetMapEntityId(args.MapId), out var weather) ||
            weather.Weather == null ||
            !_protoManager.TryIndex<WeatherPrototype>(weather.Weather, out var weatherProto))
        {
            return;
        }

        switch (args.Space)
        {
            case OverlaySpace.WorldSpaceBelowWorld:
                DrawUnderGrid(args, weatherProto);
                break;
            case OverlaySpace.WorldSpace:
                DrawWorld(args, weatherProto);
                break;
        }
    }

    private void DrawUnderGrid(in OverlayDrawArgs args, WeatherPrototype weatherProto)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        worldHandle.RenderInRenderTarget(_blep, () =>
        {
            var sprite = _sprite.Frame0(weatherProto.Sprite);
            // TODO: Handle this shit
            worldHandle.SetTransform(invMatrix);

            for (var x = worldAABB.Left; x <= worldAABB.Right; x+= (float) sprite.Width / EyeManager.PixelsPerMeter)
            {
                for (var y = worldAABB.Bottom; y <= worldAABB.Top; y+= (float) sprite.Height / EyeManager.PixelsPerMeter)
                {
                    var box = new Box2(new Vector2(x, y), new Vector2(x + sprite.Width, y + sprite.Height));

                    worldHandle.DrawTextureRect(sprite, box);
                }
            }

        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3.Identity);
    }

    private void DrawWorld(in OverlayDrawArgs args, WeatherPrototype weatherProto)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        worldHandle.RenderInRenderTarget(_blep, () =>
        {
            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
            var sprite = _sprite.Frame0(weatherProto.Sprite);

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                var (_, worldRot, matrix) = xformQuery.GetComponent(grid.GridEntityId).GetWorldPositionRotationMatrix();
                Matrix3.Multiply(in matrix, in invMatrix, out var matty);
                worldHandle.SetTransform(matty);

                foreach (var tile in grid.GetTilesIntersecting(worldAABB))
                {
                    // TODO: Exclusivity
                    var gridTile = new Box2(tile.GridIndices * grid.TileSize,
                        (tile.GridIndices + Vector2i.One) * grid.TileSize);

                    worldHandle.DrawTextureRect(sprite, new Box2Rotated(gridTile, -worldRot, gridTile.Center));
                }
            }

        }, null);

        worldHandle.SetTransform(Matrix3.Identity);
        worldHandle.DrawTextureRect(_blep.Texture, worldAABB);
    }
}
