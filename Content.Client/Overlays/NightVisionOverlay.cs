using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Fullscreen overlay that applies the night-vision shader to the rendered screen.
/// </summary>
public sealed partial class NightVisionOverlay : Overlay
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "NightVision";
    private static readonly ProtoId<ShaderPrototype> AccessibleShader = "NightVisionAccessible";
    private readonly ShaderInstance _nightVisionShader;
    private readonly ShaderInstance _nightVisionAccessibleShader;
    private ShaderInstance _activeShader;

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
        _nightVisionAccessibleShader = _prototypeManager.Index(AccessibleShader).InstanceUnique();
        _activeShader = _nightVisionShader;
        _cfg.OnValueChanged(CCVars.StaticNightVisionNoise, OnNightVisionNoiseChanged, invokeImmediately: true);
    }

    private void OnNightVisionNoiseChanged(bool toggle)
    {
        _activeShader = toggle
            ? _nightVisionAccessibleShader
            : _nightVisionShader;
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
            _activeShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _activeShader.SetParameter("noise_amount", NoiseAmount);
            _activeShader.SetParameter("noise_multiplier", NoiseMultiplier);
            handle.UseShader(_activeShader);
        }

        var drawingColor = isSpace ? OverlayColor : LightingColor;
        handle.DrawRect(args.WorldBounds, drawingColor);

        if (isSpace)
            handle.UseShader(null);
    }
}
