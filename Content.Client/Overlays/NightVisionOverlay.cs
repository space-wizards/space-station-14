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

    public Color OverlayColor { get; private set; } = Color.White;
    public Color LightingColor { get; private set; } = Color.White;
    public float NoiseAmount { get; private set; }
    public float NoiseMultiplier { get; private set; }

    public override OverlaySpace Space => OverlaySpace.BeforeLighting | OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index(Shader).InstanceUnique();
    }

    public void SetParameters(Color overlayColor, Color lightingColor, float noiseAmount, float noiseMultiplier)
    {
        OverlayColor = overlayColor;
        LightingColor = lightingColor;
        NoiseAmount = noiseAmount;
        NoiseMultiplier = noiseMultiplier;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var isSpace = args.Space == OverlaySpace.WorldSpace;

        if (isSpace)
        {
            _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _nightVisionShader.SetParameter("overlay_color", OverlayColor);
            _nightVisionShader.SetParameter("noise_amount", NoiseAmount);
            _nightVisionShader.SetParameter("noise_multiplier", NoiseMultiplier);
            handle.UseShader(_nightVisionShader);
        }

        var drawingColor = isSpace ? OverlayColor : LightingColor;
        handle.DrawRect(args.WorldBounds, drawingColor);

        if (isSpace)
            handle.UseShader(null);
    }
}
