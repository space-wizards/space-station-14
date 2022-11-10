using System.Linq;
using Content.Client.Parallax;
using Content.Shared.Weather;
using OpenToolkit.Graphics.ES11;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Weather;

public sealed class WeatherOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    private readonly SpriteSystem _sprite;
    private readonly WeatherSystem _weather;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IRenderTexture _blep;
    private IRenderTexture _blep2;

    public WeatherOverlay(SpriteSystem sprite, WeatherSystem weather)
    {
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _weather = weather;
        _sprite = sprite;
        IoCManager.InjectDependencies(this);

        var clyde = IoCManager.Resolve<IClyde>();
        _blep = clyde.CreateRenderTarget(clyde.ScreenSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather-stencil");
        _blep2 = clyde.CreateRenderTarget(clyde.ScreenSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather");
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
        var mapUid = _mapManager.GetMapEntityId(args.MapId);

        if (!_entManager.TryGetComponent<WeatherComponent>(mapUid, out var weather) ||
            weather.Weather == null ||
            !_protoManager.TryIndex<WeatherPrototype>(weather.Weather, out var weatherProto))
        {
            return;
        }

        var alpha = _weather.GetPercent(weather, mapUid, weatherProto);
        DrawWorld(args, weatherProto, alpha);
    }

    private void DrawWorld(in OverlayDrawArgs args, WeatherPrototype weatherProto, float alpha)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        if (_blep.Texture.Size != args.Viewport.Size)
        {
            _blep.Dispose();
            _blep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather-stencil");
        }

        if (_blep2.Texture.Size != args.Viewport.Size)
        {
            _blep2.Dispose();
            _blep2 = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather");
        }

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        worldHandle.RenderInRenderTarget(_blep, () =>
        {
            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                var matrix = xformQuery.GetComponent(grid.GridEntityId).WorldMatrix;
                Matrix3.Multiply(in matrix, in invMatrix, out var matty);
                worldHandle.SetTransform(matty);

                foreach (var tile in grid.GetTilesIntersecting(worldAABB))
                {
                    var tileDef = _tileDefManager[tile.Tile.TypeId];

                    // Ignored tiles for stencil
                    if (weatherProto.Tiles.Contains(tileDef.ID))
                        continue;

                    var gridTile = new Box2(tile.GridIndices * grid.TileSize,
                        (tile.GridIndices + Vector2i.One) * grid.TileSize);

                    worldHandle.DrawRect(gridTile, Color.White);
                }
            }

        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3.Identity);
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_blep.Texture, worldBounds);
        Texture? sprite = null;
        var curTime = IoCManager.Resolve<IGameTiming>().RealTime;
        // TODO: Cache this shit.

        switch (weatherProto.Sprite)
        {
            case SpriteSpecifier.Rsi rsi:
                var rsiActual = IoCManager.Resolve<IResourceCache>().GetResource<RSIResource>(rsi.RsiPath).RSI;
                rsiActual.TryGetState(rsi.RsiState, out var state);
                var frames = state!.GetFrames(RSI.State.Direction.South);
                var delays = state.GetDelays();
                var totalDelay = delays.Sum();
                var time = curTime.TotalSeconds % totalDelay;
                var delaySum = 0f;

                for (var i = 0; i < delays.Length; i++)
                {
                    var delay = delays[i];
                    delaySum += delay;

                    if (time > delaySum)
                        continue;

                    sprite = frames[i];
                    break;
                }

                sprite ??= _sprite.Frame0(weatherProto.Sprite);
                break;
            case SpriteSpecifier.Texture texture:
                sprite = texture.GetTexture(IoCManager.Resolve<IResourceCache>());
                break;
            default:
                throw new NotImplementedException();
        }

        var viewport = args.Viewport;

        // Draw the rain
        worldHandle.RenderInRenderTarget(_blep2, () =>
        {
            worldHandle.SetTransform(invMatrix);

            // var layers = _parallax.GetParallaxLayers(args.MapId);
            // var realTime = (float) _timing.RealTime.TotalSeconds;

            // Size of the texture in world units.
            var size = sprite.Size / (float) EyeManager.PixelsPerMeter;

            // The "home" position is the effective origin of this layer.
            // Parallax shifting is relative to the home, and shifts away from the home and towards the Eye centre.
            // The effects of this are such that a slowness of 1 anchors the layer to the centre of the screen, while a slowness of 0 anchors the layer to the world.
            // (For values 0.0 to 1.0 this is in effect a lerp, but it's deliberately unclamped.)
            // The ParallaxAnchor adapts the parallax for station positioning and possibly map-specific tweaks.
            var home = Vector2.Zero; // layer.Config.WorldHomePosition + _manager.ParallaxAnchor;
            var scrolled = 0f; //layer.Config.Scrolling * realTime;

            // Origin - start with the parallax shift itself.
            var originBL = (position - home) * 0.1f + scrolled;

            // Place at the home.
            originBL += home;

            // Adjust.
            // originBL += layer.Config.WorldAdjustPosition;

            // Centre the image.
            originBL -= size / 2;

            // Remove offset so we can floor.
            var flooredBL = worldAABB.BottomLeft - originBL;

            // Floor to background size.
            flooredBL = (flooredBL / size).Floored() * size;

            // Re-offset.
            flooredBL += originBL;


            for (var x = flooredBL.X; x < worldAABB.Right; x += size.X)
            {
                for (var y = flooredBL.Y; y < worldAABB.Top; y += size.Y)
                {
                    var box = Box2.FromDimensions((x, y), size);
                    worldHandle.DrawTextureRect(sprite, Box2.FromDimensions((x, y), size));
                }
            }

        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3.Identity);

        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilDraw").Instance());

        worldHandle.DrawTextureRect(_blep2.Texture, worldBounds, Color.White.WithAlpha(alpha));
        worldHandle.UseShader(null);
    }
}
