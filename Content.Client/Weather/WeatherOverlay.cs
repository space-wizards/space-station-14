using Content.Client.Parallax;
using Content.Shared.Weather;
using OpenToolkit.Graphics.ES11;
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

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IRenderTexture _blep;
    private IRenderTexture _blep2;

    public WeatherOverlay(SpriteSystem sprite)
    {
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
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
        if (!_entManager.TryGetComponent<WeatherComponent>(_mapManager.GetMapEntityId(args.MapId), out var weather) ||
            weather.Weather == null ||
            !_protoManager.TryIndex<WeatherPrototype>(weather.Weather, out var weatherProto))
        {
            return;
        }

        switch (args.Space)
        {
            case OverlaySpace.WorldSpaceBelowFOV:
                DrawWorld(args, weatherProto);
                break;
        }
    }

    private void DrawWorld(in OverlayDrawArgs args, WeatherPrototype weatherProto)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        // Cut out the irrelevant bits via stencil
        worldHandle.RenderInRenderTarget(_blep, () =>
        {
            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                var (_, worldRot, matrix) = xformQuery.GetComponent(grid.GridEntityId).GetWorldPositionRotationMatrix();
                Matrix3.Multiply(in matrix, in invMatrix, out var matty);
                worldHandle.SetTransform(matty);

                foreach (var tile in grid.GetTilesIntersecting(worldAABB))
                {
                    var gridTile = new Box2(tile.GridIndices * grid.TileSize,
                        (tile.GridIndices + Vector2i.One) * grid.TileSize);

                    worldHandle.DrawRect(new Box2Rotated(gridTile, -worldRot, gridTile.Center), Color.White);
                }
            }

        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3.Identity);
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_blep.Texture, worldBounds);

        // Draw the rain
        worldHandle.RenderInRenderTarget(_blep2, () =>
        {
            var sprite = _sprite.Frame0(weatherProto.Sprite);
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
            var originBL = (position - home) * 1f + scrolled;

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
                    worldHandle.DrawTextureRect(sprite, Box2.FromDimensions((x, y), size));
                }
            }

        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3.Identity);

        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilDraw").Instance());
        worldHandle.DrawTextureRect(_blep2.Texture, worldBounds);
        worldHandle.UseShader(null);
    }
}
