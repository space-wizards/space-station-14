using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Fullscreen overlay that applies the night-vision shader to the rendered screen.
/// </summary>
public sealed partial class NightVisionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "NightVision";

    private readonly Color _color;
    private readonly float _noiseAmount;
    private readonly float _noiseMultiplier;

    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _nightVisionShader;

    public NightVisionOverlay(Color color, float noiseAmount, float noiseMultiplier)
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index(Shader).InstanceUnique();

        _color = color;
        _noiseAmount = noiseAmount;
        _noiseMultiplier = noiseMultiplier;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _nightVisionShader.SetParameter("noise_amount", _noiseAmount);
        _nightVisionShader.SetParameter("noise_multiplier", _noiseMultiplier);
        handle.UseShader(_nightVisionShader);
        handle.DrawRect(args.WorldBounds, _color);
        handle.UseShader(null);
    }
}
