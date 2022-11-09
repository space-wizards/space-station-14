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

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

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
            case OverlaySpace.WorldSpace:
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
        worldHandle.SetTransform(Matrix3.Identity);

        /*
         * NOTE!
         * Render targets are in screenspace.
         */
        worldHandle.SetTransform(Matrix3.CreateRotation(-rotation));

        // Draw the rain
        worldHandle.RenderInRenderTarget(_blep, () =>
        {
            var sprite = _sprite.Frame0(weatherProto.Sprite);
            // worldHandle.SetTransform(Matrix3.Identity);
            var spriteDimensions = (Vector2) sprite.Size / EyeManager.PixelsPerMeter;
            var textureDimensions = (Vector2) _blep.Texture.Size / EyeManager.PixelsPerMeter;

            for (var x = 0f; x < _blep.Texture.Width; x += sprite.Width)
            {
                for (var y = 0f; y < _blep.Texture.Height; y += sprite.Height)
                {
                    var botLeft = new Vector2(x, y);
                    var box = Box2.FromDimensions(botLeft, botLeft + spriteDimensions);
                    worldHandle.DrawTextureRect(sprite, box);
                }
            }

        }, Color.Transparent);

        // Cut out the irrelevant bits.

        /*
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
                    // TODO: Exclusivity
                    var gridTile = new Box2(tile.GridIndices * grid.TileSize,
                        (tile.GridIndices + Vector2i.One) * grid.TileSize);

                    worldHandle.DrawRect(new Box2Rotated(gridTile, -worldRot, gridTile.Center), Color.Black);
                }
            }

        }, Color.Transparent);
        */

        worldHandle.SetTransform(Matrix3.Identity);
        worldHandle.DrawTextureRect(_blep.Texture, worldBounds);
        worldHandle.SetTransform(Matrix3.Identity);
    }
}
