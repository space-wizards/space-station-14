using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class StencilOverlay
{
    private void DrawWeather(
        in OverlayDrawArgs args,
        HashSet<Entity<WeatherStatusEffectComponent, StatusEffectComponent>> weathers)
    {
        var worldHandle = args.WorldHandle;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        var stencil = _gridStencil.GetTileStencil(args,
            "weather-blocked",
            "weather-blocked-grid-stencil",
            (grid, tile) =>
            {
                // Ignored tiles for stencil.
                return !_weather.CanWeatherAffect((grid.Owner, grid.Comp, null), tile);
            });

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
        worldHandle.DrawTextureRect(stencil.Texture, worldBounds);
        var curTime = _timing.RealTime;

        foreach (var (uid, weather, status) in weathers)
        {
            var alpha = _weather.GetWeatherPercent((uid, status));
            var sprite = _sprite.GetFrame(weather.Sprite, curTime);

            // Draw the rain
            worldHandle.UseShader(_protoManager.Index(StencilDraw).Instance());
            _parallax.DrawParallax(worldHandle,
                worldAABB,
                sprite,
                curTime,
                position,
                weather.Scrolling ?? Vector2.Zero,
                modulate: (weather.Color ?? Color.White).WithAlpha(alpha));
        }

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }
}
