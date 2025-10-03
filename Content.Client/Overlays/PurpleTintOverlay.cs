using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Client.Overlays;

public sealed partial class PurpleTintOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "PurpleTint";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    // Parameters (neutral defaults; map component config overrides these)
    public Color TintColor { get; set; } = Color.White;
    public float Strength { get; set; } = 0.0f;
    public float Saturation { get; set; } = 1.0f;
    public float Contrast { get; set; } = 1.0f;

    public PurpleTintOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShaderId).InstanceUnique();
        // Draw above most world effects but before post UI passes
        ZIndex = 9;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tintColor", new Vector3(TintColor.R, TintColor.G, TintColor.B));
        _shader.SetParameter("strength", Strength);
        _shader.SetParameter("saturation", Saturation);
        _shader.SetParameter("contrast", Contrast);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
