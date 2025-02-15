using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Light;

/// <summary>
/// Essentially handles blurring for content-side light overlays.
/// </summary>
public sealed class LightBlurOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IClyde _clyde = default!;

    public const int ContentZIndex = TileEmissionOverlay.ContentZIndex + 1;

    public LightBlurOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = ContentZIndex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var target = IoCManager.Resolve<IOverlayManager>().GetOverlay<BeforeLightTargetOverlay>().EnlargedLightTarget;
        // Yeah that's all this does keep walkin.
        //_clyde.BlurRenderTarget(args.Viewport, target, args.Viewport.Eye, 14f * 5f);
    }
}
