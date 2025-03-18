using System.Numerics;
using Content.Shared._EE.Overlays.Switchable;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._EE.Overlays.Switchable;

public sealed class BaseSwitchableOverlay<TComp> : Overlay where TComp : SwitchableOverlayComponent
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public TComp? Comp = null;

    public bool IsActive = true;

    public BaseSwitchableOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index<ShaderPrototype>("NightVision").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || Comp is null || !IsActive)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tint", Comp.Tint);
        _shader.SetParameter("luminance_threshold", Comp.Strength);
        _shader.SetParameter("noise_amount", Comp.Noise);

        var worldHandle = args.WorldHandle;

        var accumulator = Math.Clamp(Comp.PulseAccumulator, 0f, Comp.PulseTime);
        var alpha = Comp.PulseTime <= 0f ? 0.6f : float.Lerp(1f, 0f, accumulator / Comp.PulseTime);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Comp.Color.WithAlpha(alpha));
        worldHandle.UseShader(null);
    }
}
