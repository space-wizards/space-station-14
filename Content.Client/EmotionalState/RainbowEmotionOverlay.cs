using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class RainbowEmotionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "RainbowEmotion";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _rainbowEmotionShader;

    public RainbowEmotionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _rainbowEmotionShader = _prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = 9; // draw this over the DamageOverlay, RainbowOverlay etc, but before the black and white shader
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _rainbowEmotionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_rainbowEmotionShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
