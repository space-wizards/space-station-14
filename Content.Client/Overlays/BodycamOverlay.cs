using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.Overlays;

public sealed class BodycamOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "Bodycam";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    // Tunable parameters
    public float ScanlineStrength { get; set; } = 0.6f;
    public float VignetteStrength { get; set; } = 0.7f;
    public float AberrationStrength { get; set; } = 0.0f; // disable color fringe/outline by default
    public float GrainStrength { get; set; } = 0.35f;
    public float DistortionStrength { get; set; } = 0.05f;
    public float CornerRadius { get; set; } = 0.03f;   // UV units (~3% of width/height)
    public float CornerFeather { get; set; } = 0.01f;  // soft edge

    public BodycamOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShaderId).InstanceUnique();
        ZIndex = 9;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("time", (float) _timing.CurTime.TotalSeconds);
        _shader.SetParameter("scanlineStrength", ScanlineStrength);
        _shader.SetParameter("vignetteStrength", VignetteStrength);
        _shader.SetParameter("aberrationStrength", AberrationStrength);
        _shader.SetParameter("grainStrength", GrainStrength);
        _shader.SetParameter("distortionStrength", DistortionStrength);
        _shader.SetParameter("cornerRadius", CornerRadius);
        _shader.SetParameter("cornerFeather", CornerFeather);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
