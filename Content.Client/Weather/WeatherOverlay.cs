using System.Linq;
using System.Numerics;
using Content.Client.Parallax;
using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
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
    [Dependency] private readonly IResourceCache _cache = default!;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly WeatherSystem _weather;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IRenderTexture? _blep;

    public WeatherOverlay(SharedTransformSystem transform, SpriteSystem sprite, WeatherSystem weather)
    {
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _transform = transform;
        _weather = weather;
        _sprite = sprite;
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        if (!_entManager.TryGetComponent<WeatherComponent>(_mapManager.GetMapEntityId(args.MapId), out var weather) ||
            weather.Weather.Count == 0)
        {
            return false;
        }

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mapUid = _mapManager.GetMapEntityId(args.MapId);

        if (!_entManager.TryGetComponent<WeatherComponent>(mapUid, out var comp))
        {
            return;
        }

        foreach (var (proto, weather) in comp.Weather)
        {
            if (!_protoManager.TryIndex<WeatherPrototype>(proto, out var weatherProto))
                continue;

            var alpha = _weather.GetPercent(weather, mapUid);
            DrawWorld(args, weatherProto, alpha);
        }
    }

    private void DrawWorld(in OverlayDrawArgs args, WeatherPrototype weatherProto, float alpha)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        if (_blep?.Texture.Size != args.Viewport.Size)
        {
            _blep?.Dispose();
            _blep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather-stencil");
        }

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        worldHandle.RenderInRenderTarget(_blep, () =>
        {
            var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();
            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
            var weatherIgnoreQuery = _entManager.GetEntityQuery<IgnoreWeatherComponent>();

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                var matrix = _transform.GetWorldMatrix(grid.Owner, xformQuery);
                Matrix3.Multiply(in matrix, in invMatrix, out var matty);
                worldHandle.SetTransform(matty);

                foreach (var tile in grid.GetTilesIntersecting(worldAABB))
                {
                    // Ignored tiles for stencil
                    if (_weather.CanWeatherAffect(grid, tile, weatherIgnoreQuery, bodyQuery))
                    {
                        continue;
                    }

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
        var curTime = _timing.RealTime;

        switch (weatherProto.Sprite)
        {
            case SpriteSpecifier.Rsi rsi:
                var rsiActual = _cache.GetResource<RSIResource>(rsi.RsiPath).RSI;
                rsiActual.TryGetState(rsi.RsiState, out var state);
                var frames = state!.GetFrames(RsiDirection.South);
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
                sprite = texture.GetTexture(_cache);
                break;
            default:
                throw new NotImplementedException();
        }

        // Draw the rain
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilDraw").Instance());

        // TODO: This is very similar to parallax but we need stencil support but we can probably combine these somehow
        // and not make it spaghetti, while getting the advantages of not-duped code?


        // Okay I have spent like 5 hours on this at this point and afaict you have one of the following comprises:
        // - No scrolling so the weather is always centered on the player
        // - Crappy looking rotation but strafing looks okay and scrolls
        // - Crappy looking strafing but rotation looks okay.
        // - No rotation
        // - Storing state across frames to do scrolling and just having it always do topdown.

        // I have chosen no rotation.

        const float scale = 1f;
        const float slowness = 0f;
        var scrolling = Vector2.Zero;

        // Size of the texture in world units.
        var size = (sprite.Size / (float) EyeManager.PixelsPerMeter) * scale;
        var scrolled = scrolling * (float) curTime.TotalSeconds;

        // Origin - start with the parallax shift itself.
        var originBL = position * slowness + scrolled;

        // Centre the image.
        originBL -= size / 2;

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft - originBL;

        // Floor to background size.
        flooredBL = (flooredBL / size).Floored() * size;

        // Re-offset.
        flooredBL += originBL;

        for (var x = flooredBL.X; x < args.WorldAABB.Right; x += size.X)
        {
            for (var y = flooredBL.Y; y < args.WorldAABB.Top; y += size.Y)
            {
                var box = Box2.FromDimensions(new Vector2(x, y), size);
                worldHandle.DrawTextureRect(sprite, box, (weatherProto.Color ?? Color.White).WithAlpha(alpha));
            }
        }

        worldHandle.SetTransform(Matrix3.Identity);
        worldHandle.UseShader(null);
    }
}
