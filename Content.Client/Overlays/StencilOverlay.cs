using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Parallax;
using Content.Client.Weather;
using Content.Shared.Salvage;
using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Overlays;

/// <summary>
/// Simple re-useable overlay with stencilled texture.
/// </summary>
public sealed partial class StencilOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CircleShader = "WorldGradientCircle";
    private static readonly ProtoId<ShaderPrototype> StencilMask = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDraw = "StencilDraw";

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    private readonly ParallaxSystem _parallax;
    private readonly SharedTransformSystem _transform;
    private readonly SharedMapSystem _map;
    private readonly SpriteSystem _sprite;
    private readonly WeatherSystem _weather;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly OverlayResourceCache<CachedResources> _resources = new();

    private readonly ShaderInstance _shader;

    public StencilOverlay(ParallaxSystem parallax, SharedTransformSystem transform, SharedMapSystem map, SpriteSystem sprite, WeatherSystem weather)
    {
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _parallax = parallax;
        _transform = transform;
        _map = map;
        _sprite = sprite;
        _weather = weather;
        IoCManager.InjectDependencies(this);
        _shader = _protoManager.Index(CircleShader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mapUid = _map.GetMapOrInvalid(args.MapId);
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.Blep?.Texture.Size != args.Viewport.Size)
        {
            res.Blep?.Dispose();
            res.Blep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather-stencil");
        }

        if (_entManager.TryGetComponent<WeatherComponent>(mapUid, out var comp))
        {
            foreach (var (proto, weather) in comp.Weather)
            {
                if (!_protoManager.Resolve<WeatherPrototype>(proto, out var weatherProto))
                    continue;

                var alpha = _weather.GetPercent(weather, mapUid);
                DrawWeather(args, res, weatherProto, alpha, invMatrix);
            }
        }

        if (_entManager.TryGetComponent<RestrictedRangeComponent>(mapUid, out var restrictedRangeComponent))
        {
            DrawRestrictedRange(args, res, restrictedRangeComponent, invMatrix);
        }

        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();

        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? Blep;

        public void Dispose()
        {
            Blep?.Dispose();
        }
    }
}
