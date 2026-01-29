using System.Numerics;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class NightVisionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "NightVision";

    private readonly NightVisionComponent _component;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _nightVisionShader;


    public NightVisionOverlay(NightVisionComponent component)
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index(Shader).InstanceUnique();
        _component = component;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _nightVisionShader.SetParameter("tint", _component.Tint);
        _nightVisionShader.SetParameter("luminance_threshold", _component.LuminanceThreshold);
        _nightVisionShader.SetParameter("noise_amount", _component.NoiseAmount);
        handle.UseShader(_nightVisionShader);
        handle.DrawRect(args.WorldBounds, Color.FromHex(_component.Color));
        handle.UseShader(null);
    }
}
