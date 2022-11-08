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
                // DrawUnderGrid(args, weatherProto);
                break;
            case OverlaySpace.WorldSpace:
                DrawWorld(args, weatherProto);
                break;
        }
    }

    private void DrawUnderGrid(in OverlayDrawArgs args, WeatherPrototype weatherProto)
    {
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("unshaded").Instance());

        var sprite = _sprite.Frame0(weatherProto.Sprite);
        var worldWidth = sprite.Width / EyeManager.PixelsPerMeter;
        var worldHeight = sprite.Height / EyeManager.PixelsPerMeter;

        for (var x = args.WorldAABB.Left; x < args.WorldAABB.Right; x += worldWidth)
        {
            for (var y = args.WorldAABB.Bottom; y < args.WorldAABB.Top; y += worldHeight)
            {
                var tile = new Box2(new Vector2(x, y), new Vector2(x + worldWidth, y + worldHeight));
                worldHandle.DrawTextureRect(sprite, new Box2Rotated(tile, -eyeRotation, tile.Center));
            }
        }

        worldHandle.SetTransform(Matrix3.Identity);
    }

    private void DrawWorld(in OverlayDrawArgs args, WeatherPrototype weatherProto)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;

        worldHandle.RenderInRenderTarget(_blep, () =>
        {
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                var matrix = _entManager.GetComponent<TransformComponent>(grid.GridEntityId).WorldMatrix;
                worldHandle.SetTransform(matrix);
                // TODO: Need to transfer to worldspace in viewport.

                // TODO: For each tile on grid.
                foreach (var tile in grid.GetTilesIntersecting(worldBounds))
                {
                    // TODO: Exclusivity

                    worldHandle.DrawTextureRect(_sprite.Frame0(weatherProto.Sprite), new Box2(tile.GridIndices * grid.TileSize, (tile.GridIndices + Vector2i.One) * grid.TileSize));
                }
            }

            worldHandle.SetTransform(Matrix3.Identity);
        }, Color.Transparent);

        worldHandle.DrawTextureRect(_blep.Texture, args.WorldAABB);
    }
}
