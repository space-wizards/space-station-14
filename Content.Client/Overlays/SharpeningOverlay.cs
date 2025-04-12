using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Overlays;

public sealed class SharpeningOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly ShaderInstance _shader;

    public float Sharpness { get; set; } = 0f;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;
    
    public SharpeningOverlay()
    {
        IoCManager.InjectDependencies(this);
        
        _shader = _proto.Index<ShaderPrototype>("Sharpening").InstanceUnique();
        ZIndex = -5;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (Sharpness <= 0.0f || _playerManager.LocalEntity == null)
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null || ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("Sharpness", Sharpness);
        
        var handle = args.ScreenHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.ViewportBounds, Color.White);
        handle.UseShader(null);
    }
}
