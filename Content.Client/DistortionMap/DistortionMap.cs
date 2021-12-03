using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;

namespace Content.Client.DistortionMap;

class DistortionMapOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private Dictionary<IClydeViewport, DistMapInstance> _maps = new();
    private ShaderInstance? _shd = default;

    public DistortionMapOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = 99999;

        _shd = _prototypeManager
            .Index<ShaderPrototype>("Distortion")
            .Instance()
            .Duplicate();

        if (_shd is null)
            throw new Exception("unable to create distortion map shader");
    }

    public const float MapSizeDivisor = 2.0f;

    public DistMapInstance? CreateMap(IClydeViewport vp, float szDiv = MapSizeDivisor)
    {
        var rtfp = new RenderTargetFormatParameters();
        rtfp.ColorFormat = RenderTargetColorFormat.RG32F;

        var tsp = new TextureSampleParameters();
        tsp.Filter = true;

        var vs = vp.Size / szDiv;
        var rt = _clyde.CreateRenderTarget(
                new Vector2i((int) vs.X, (int) vs.Y),
                rtfp,
                tsp
        );

        if (rt is null)
            throw new Exception("unable to create distortion map render target");

        var dmi = new DistMapInstance(rt, szDiv) { used = true };
        _maps[vp] = dmi;
        return dmi;
    }

    public bool TryGetMap(IClydeViewport vp, [NotNullWhen(true)] out DistMapInstance? map)
    {
        if (_maps.TryGetValue(vp, out var mi))
        {
            mi.used = true;
            map = mi;
            return true;
        }
        map = default;
        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_maps.TryGetValue(args.Viewport, out var map))
            return;

        if (!map.used)
            return;

        map.used = false;
        _shd?.SetParameter("Scr", ScreenTexture!);
        _shd?.SetParameter("ScrSz", ScreenTexture!.Size);
        _shd?.SetParameter("Map", map.map.Texture);
        _shd?.SetParameter("MapSz", map.map.Texture.Size);
        args.WorldHandle.UseShader(_shd);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
    }

    public new void Dispose()
    {
        base.Dispose();

        foreach (var v in _maps.Values)
            v.map.Dispose();

        if (_shd is not null)
            _shd.Dispose();
    }

    public class DistMapInstance
    {
        public IRenderTexture map;

        public bool used = false;
        public float szDiv;

        public DistMapInstance(IRenderTexture rt, float div)
        {
            map = rt;
            szDiv = div;
        }
    }
}
