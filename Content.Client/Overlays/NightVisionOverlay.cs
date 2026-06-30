using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Fullscreen overlay that applies the night-vision shader to the rendered screen.
/// </summary>
public sealed partial class NightVisionOverlay : Overlay
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "NightVision";
    private readonly ShaderInstance _nightVisionShader;

    public Color ColorShader;
    public Color ColorLighting;
    public float NoiseAmount;
    public float NoiseMultiplier;

    public override OverlaySpace Space => OverlaySpace.BeforeLighting | OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public NightVisionOverlay(Color colorShader, Color colorLighting, float noiseAmount, float noiseMultiplier)
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index(Shader).InstanceUnique();

        ColorShader = colorShader;
        ColorLighting = colorLighting;
        NoiseAmount = noiseAmount;
        NoiseMultiplier = noiseMultiplier;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var drawingSpace = args.Space == OverlaySpace.WorldSpace;

        var drawingColor = drawingSpace ? ColorShader : ColorLighting;

        if (drawingSpace)
        {
            _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _nightVisionShader.SetParameter("noise_amount", NoiseAmount);
            _nightVisionShader.SetParameter("noise_multiplier", NoiseMultiplier);
            handle.UseShader(_nightVisionShader);
        }

        handle.DrawRect(args.WorldBounds, drawingColor);

        if (drawingSpace)
            handle.UseShader(null);
    }
}
